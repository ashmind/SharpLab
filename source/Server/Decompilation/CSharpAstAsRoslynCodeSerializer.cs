using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using AshMind.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MirrorSharp.Advanced;
using Mono.Reflection;
using SharpLab.Server.Decompilation.AstOnly;

namespace SharpLab.Server.Decompilation {
    public class CSharpAstAsRoslynCodeSerializer {
        private readonly IReadOnlyList<FactoryMethod> _factoryMethods = MapFactoryAsync().Result;

        public void SerializeAstAsCode(object ast, IFastJsonWriter writer, IWorkSession session) {
            using (var stringWriter = writer.OpenString()) {
                SerializeSyntax(((RoslynAst)ast).SyntaxRoot, stringWriter, session, "");
            }
        }

        private static readonly IReadOnlyList<SyntaxKind> SyntaxKinds =
           Enum.GetValues(typeof(SyntaxKind))
               .Cast<SyntaxKind>()
               .ToList();

        private static readonly IReadOnlyList<SyntaxKind> TokenKinds =
            SyntaxKinds
                .Where(k => {
                    var name = k.ToString();
                    return name.EndsWith("Token") || name.EndsWith("Keyword");
                })
                .ToList();

        private static async Task<IReadOnlyList<FactoryMethod>> MapFactoryAsync() {
            var methods = typeof(SyntaxFactory)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(ShouldMapFactoryMethod)
                .ToList();

            var createdByType = methods
                .Select(m => m.ReturnType)
                .Distinct()
                .ToDictionary(t => t, t => new TaskCompletionSource<object>());

            return await Task.WhenAll(
                methods.Select(m => Task.Run(() => MapFactoryMethodAsync(m, createdByType)))
            );
        }

        private static bool ShouldMapFactoryMethod(MethodInfo method) {
            if (method.IsGenericMethodDefinition)
                return false;
            if (method.Name.StartsWith("Get") && method.Name.EndsWith("Expression"))
                return false;
            if (method.Name.StartsWith("Parse"))
                return false;
            if (method.Name == nameof(SyntaxFactory.TypeDeclaration))
                return false;
            if (!method.ReturnType.IsSameAsOrSubclassOf<SyntaxNode>())
                return false;

            return true;
        }

        private static async Task<FactoryMethod> MapFactoryMethodAsync(MethodInfo method, IReadOnlyDictionary<Type, TaskCompletionSource<object>> createdByType) {
            var parameters = method.GetParameters();
            var info = new FactoryMethod(method, parameters);
            var arguments = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++) {
                arguments[i] = await GetStubArgumentAsync(parameters[i], createdByType);
            }

            var created = method.Invoke(null, arguments);
            createdByType[method.ReturnType].TrySetResult(created);

            var parameterNames = parameters.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var property in created.GetType().GetProperties()) {
                if (ShouldSkipProperty(property))
                    continue;

                if (parameterNames.TryGetValue(property.Name, out var parameter)) {
                    info.ParameterPropertyMap.Add(parameter, property);
                    continue;
                }

                object value;
                try {
                    value = property.GetValue(created);
                }
                catch (TargetInvocationException ex) when (ex.InnerException is InvalidCastException) {
                    // happens in some cases, e.g. AnonymousMethodExpressionSyntax.Block when body is not a block
                    continue;
                }

                info.Defaults.Add(new PropertyValue(property, CleanUpDefaultValue(value)));
            }
            return info;
        }

        private static async ValueTask<object> GetStubArgumentAsync(ParameterInfo parameter, IReadOnlyDictionary<Type, TaskCompletionSource<object>> createdByType) {
            if (parameter.HasDefaultValue)
                return parameter.DefaultValue;

            var type = parameter.ParameterType;

            if (type == typeof(string))
                return "_";

            if (type == typeof(Uri))
                return new Uri("urn:_");

            if (type.IsArray)
                return Array.CreateInstance(parameter.ParameterType.GetElementType(), 0);

            if (type == typeof(SyntaxTokenList))
                return SyntaxFactory.TokenList();

            if (type.IsGenericType) {
                var typeDefinition = type.GetGenericTypeDefinition();
                var typeArguments = type.GetGenericArguments();
                if (typeDefinition == typeof(IEnumerable<>))
                    return Array.CreateInstance(typeArguments[0], 0);

                if (typeDefinition == typeof(SyntaxList<>))
                    return typeof(SyntaxFactory).GetMethod(nameof(SyntaxFactory.List), Type.EmptyTypes).MakeGenericMethod(typeArguments[0]).Invoke(null, null);
            }

            var source = createdByType.FirstOrDefault(p => p.Key.IsAssignableTo(parameter.ParameterType)).Value;
            if (source != null)
                return await source.Task;

            if (type == typeof(SyntaxToken)) {
                var name = parameter.Name;
                if (name == "identifier")
                    return SyntaxFactory.Identifier("_");

                if (name.Contains("Or"))
                    name = name.Split("Or").Last();

                var kind = TokenKinds.FirstOrDefault(k => k.ToString().Equals(name, StringComparison.OrdinalIgnoreCase));
                if (kind == default)
                    kind = FindExpectedKind((MethodInfo)parameter.Member, parameter);

                return SyntaxFactory.Token(kind);
            }

            if (type == typeof(SyntaxKind)) {
                if (parameter.Name == "quoteKind")
                    return SyntaxKind.DoubleQuoteToken;
                return FindExpectedKind((MethodInfo)parameter.Member, parameter);
            }

            return null;
        }

        private static SyntaxKind FindExpectedKind(MethodInfo method, ParameterInfo parameter) {
            var instructions = method.GetInstructions();
            foreach (var instruction in instructions) {
                if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodInfo callee && callee.DeclaringType == method.DeclaringType && callee.Name == method.Name) {
                    var calleeParameter = callee.GetParameters().First(p => p.Name == parameter.Name);
                    return FindExpectedKind(callee, calleeParameter);
                }
            }

            var inArgumentScope = false;
            foreach (var instruction in instructions) {
                if (GetIndexIfLdarg(instruction) is int argumentIndex) {
                    inArgumentScope = (argumentIndex == parameter.Position);
                    continue;
                }

                if (!inArgumentScope)
                    continue;

                if (instruction.OpCode == OpCodes.Ldc_I4) {
                    var operand = (int)instruction.Operand;
                    if (operand < 100)
                        continue;
                    return (SyntaxKind)operand;
                }
            }

            throw new NotSupportedException();
        }

        private static int? GetIndexIfLdarg(Instruction instruction) {
            if (instruction.OpCode == OpCodes.Ldarg_0) return 0;
            if (instruction.OpCode == OpCodes.Ldarg_1) return 1;
            if (instruction.OpCode == OpCodes.Ldarg_2) return 2;
            if (instruction.OpCode == OpCodes.Ldarg_3) return 3;
            if (instruction.OpCode == OpCodes.Ldarg_S || instruction.OpCode == OpCodes.Ldarg)
                return ((ParameterInfo)instruction.Operand).Position;
            return null;
        }

        private static object CleanUpDefaultValue(object value) {
            switch (value) {
                case SyntaxToken token:
                    return token
                        .WithLeadingTrivia(CleanUpDefaultValue(token.LeadingTrivia))
                        .WithTrailingTrivia(CleanUpDefaultValue(token.TrailingTrivia));

                default:
                    return value;
            }
        }

        private static SyntaxTriviaList CleanUpDefaultValue(SyntaxTriviaList triviaList) {
            return SyntaxFactory.TriviaList(triviaList.Where(t => !t.Span.IsEmpty));
        }

        private void SerializeSyntax(SyntaxNode node, TextWriter writer, IWorkSession session, string indent) {
            var type = node.GetType();
            var propertiesToFill = new HashSet<PropertyValue>();
            foreach (var property in type.GetProperties()) {
                if (ShouldSkipProperty(property))
                    continue;

                propertiesToFill.Add(new PropertyValue(property, property.GetValue(node)));
            }

            var minRemainingCount = int.MaxValue;
            var minParameterCount = int.MaxValue;
            var factory = (FactoryMethod)null;
            var propertiesRemaining = (HashSet<PropertyValue>)null;
            foreach (var candidate in _factoryMethods) {
                if (candidate.ReturnType != type)
                    continue;

                var candidatePropertiesRemaining = new HashSet<PropertyValue>(propertiesToFill);
                candidatePropertiesRemaining.ExceptWith(candidate.Defaults);
                if (candidatePropertiesRemaining.Count > minRemainingCount)
                    continue;

                if (candidate.Parameters.Count > minParameterCount)
                    continue;

                factory = candidate;
                propertiesRemaining = candidatePropertiesRemaining;
                minRemainingCount = candidatePropertiesRemaining.Count;
                minParameterCount = candidate.Parameters.Count;
            }
            if (factory == null)
                throw new NotSupportedException("oh no");

            writer.Write(factory.Name);
            writer.Write("(");
            var mustUseParameterNames = false;
            var needsWriteLine = false;
            foreach (var parameter in factory.Parameters) {
                if (!factory.ParameterPropertyMap.TryGetValue(parameter, out var property))
                    throw new Exception("hmmm!");

                var entry = propertiesRemaining.First(x => x.Property == property);
                if (parameter.HasDefaultValue && CanSkipValue(entry.Value)) {
                    mustUseParameterNames = true;
                    continue;
                }
                writer.WriteLine();
                writer.Write(indent + "    ");
                needsWriteLine = true;
                if (mustUseParameterNames) {
                    writer.Write(parameter.Name);
                    writer.Write(": ");
                }
                SerializeObject(entry.Value, entry.Property.PropertyType, writer, session, indent + "    ");
                propertiesRemaining.Remove(entry);
            }
            if (needsWriteLine) {
                writer.WriteLine();
                writer.Write(indent);
            }
            writer.Write(")");
            
            foreach (var (property, value) in propertiesRemaining) {
                if (CanSkipValue(value))
                    continue;

                writer.WriteLine();
                writer.Write(indent);
                writer.Write(".With");
                writer.Write(property.Name);
                writer.WriteLine("(");
                writer.Write(indent + "    ");
                SerializeObject(value, property.PropertyType, writer, session, indent + "    ");
                writer.WriteLine();
                writer.Write(indent);
                writer.Write(")");
            }
        }

        private static bool ShouldSkipProperty(PropertyInfo property) {
            return property.DeclaringType == typeof(SyntaxNode) || property.DeclaringType == typeof(CSharpSyntaxNode);
        }

        private bool CanSkipValue(object value) {
            if (value is SyntaxTokenList l && !l.Any())
                return true;
            if (value is IEnumerable e && !e.Cast<object>().Any())
                return true;

            return false;
        }

        private void SerializeObject(object value, Type declaredType, TextWriter writer, IWorkSession session, string indent) {
            switch (value) {
                case IReadOnlyCollection<SyntaxNode> nodes when declaredType.IsGenericTypeDefinedAs(typeof(SyntaxList<>)):
                    if (nodes.Count != 1) {
                        writer.Write("List<");
                    }
                    else {
                        writer.Write("SingletonList<");
                    }
                    writer.Write(declaredType.GetGenericArguments()[0].Name);
                    writer.WriteLine(">(");
                    writer.Write(indent + "    ");

                    foreach (var node in nodes) {
                        SerializeSyntax(node, writer, session, indent + "    ");
                    }

                    writer.WriteLine();
                    writer.Write(indent);
                    writer.Write(")");
                    break;

                case SyntaxNode node:
                    SerializeSyntax(node, writer, session, indent);
                    break;

                case SyntaxTokenList tokens:
                    writer.WriteLine("TokenList(");
                    writer.Write(indent + "    ");
                    foreach (var token in tokens) {
                        SerializeToken(token, writer, indent + "    ");
                    }
                    writer.WriteLine();
                    writer.Write(indent);
                    writer.Write(")");
                    break;

                case SyntaxToken token:
                    SerializeToken(token, writer, indent);
                    break;

                case null:
                    writer.Write("null");
                    break;

                default:
                    throw new NotSupportedException(value.ToString());
            }
        }

        private static void SerializeToken(SyntaxToken token, TextWriter writer, string indent) {
            if (token.Kind() == SyntaxKind.IdentifierToken) {
                writer.Write("Identifier(");
            }
            else {
                writer.Write("Token(");
            }

            var subindent = indent + "    ";
            if (token.HasLeadingTrivia || token.HasTrailingTrivia) {
                writer.WriteLine();
                writer.Write(subindent);
                SerializeTriviaList(token.LeadingTrivia, writer, subindent);
                writer.WriteLine(",");
                writer.Write(subindent);
            }
            if (token.Kind() == SyntaxKind.IdentifierToken) {
                writer.Write('\"');
                writer.Write(token.Text);
                writer.Write('\"');
            }
            else {
                writer.Write("SyntaxKind.");
                writer.Write(token.Kind().ToString());
            }

            if (token.HasLeadingTrivia || token.HasTrailingTrivia) {
                writer.WriteLine(",");
                writer.Write(subindent);
                SerializeTriviaList(token.TrailingTrivia, writer, subindent);
                writer.WriteLine();
                writer.Write(indent);
            }
            writer.Write(")");
        }

        private static void SerializeTriviaList(SyntaxTriviaList triviaList, TextWriter writer, string indent) {
            writer.Write("TriviaList(");
            if (!triviaList.Any()) {
                writer.Write(")");
                return;
            }
            var subindent = indent + "    ";
            writer.WriteLine();
            writer.Write(subindent);
            foreach (var trivia in triviaList) {
                SerializeTrivia(trivia, writer);
            }
            writer.WriteLine();
            writer.Write(indent);
            writer.Write(")");
        }

        private static void SerializeTrivia(SyntaxTrivia trivia, TextWriter writer) {
            writer.Write("Space");
        }

        private class FactoryMethod {
            public FactoryMethod(MethodInfo method, ParameterInfo[] parameters) {
                Method = method;
                Parameters = parameters;
            }

            public MethodInfo Method { get; }
            public IReadOnlyList<ParameterInfo> Parameters { get; }

            public string Name => Method.Name;
            public Type ReturnType => Method.ReturnType;
            public ISet<PropertyValue> Defaults { get; } = new HashSet<PropertyValue>();
            public IDictionary<ParameterInfo, PropertyInfo> ParameterPropertyMap { get; } = new Dictionary<ParameterInfo, PropertyInfo>();

            public override string ToString() => Method.Name + "(" + Parameters.Count + ")";
        }

        private struct PropertyValue : IEquatable<PropertyValue> {
            public PropertyValue(PropertyInfo property, object value) {
                Property = property;
                Value = value;
            }

            public PropertyInfo Property { get; }
            public object Value { get; }

            public void Deconstruct(out PropertyInfo property, out object value) {
                property = Property;
                value = Value;
            }

            public override int GetHashCode() {
                return Property.GetHashCode()
                     ^ GetValueHashCode();
            }

            private int GetValueHashCode() {
                switch (Value) {
                    case SyntaxToken token:
                        return token.RawKind.GetHashCode()
                             ^ token.Text.GetHashCode()
                             ^ GetTriviaHashCode(token.LeadingTrivia)
                             ^ GetTriviaHashCode(token.TrailingTrivia);

                    case null:
                        return 0;

                    default:
                        return Value.GetHashCode();
                }
            }

            private int GetTriviaHashCode(SyntaxTriviaList triviaList) {
                var hashCode = 0;
                for (var i = 0; i < triviaList.Count; i++) {
                    hashCode ^= GetTriviaHashCode(triviaList[i]);
                }
                return hashCode;
            }

            private int GetTriviaHashCode(SyntaxTrivia trivia) {
                return trivia.RawKind.GetHashCode()
                     ^ GetAnnotationsHashCode(trivia.GetAnnotations());
            }

            private int GetAnnotationsHashCode(IEnumerable<SyntaxAnnotation> annotations) {
                var hashCode = 0;
                foreach (var annotation in annotations) {
                    hashCode ^= annotation.GetHashCode();
                }
                return hashCode;
            }

            public override bool Equals(object obj) {
                return obj is PropertyValue pv && Equals(pv);
            }

            public bool Equals(PropertyValue other) {
                return Property == other.Property
                    && ValueEquals(other.Value);
            }

            private bool ValueEquals(object value) {
                switch (Value) {
                    case SyntaxToken token when value is SyntaxToken other:
                        return token.RawKind == other.RawKind
                            && token.Text == other.Text
                            && !token.HasAnnotations() && !other.HasAnnotations()
                            && TriviaEquals(token.LeadingTrivia, other.LeadingTrivia)
                            && TriviaEquals(token.TrailingTrivia, other.TrailingTrivia);

                    default:
                        return Equals(Value, value);
                }
            }

            private bool TriviaEquals(SyntaxTriviaList triviaList, SyntaxTriviaList otherList) {
                if (triviaList.Count != otherList.Count)
                    return false;
                for (var i = 0; i < triviaList.Count; i++) {
                    if (triviaList[i] != otherList[i])
                        return false;
                }
                return true;
            }

            private bool TriviaEquals(SyntaxTrivia trivia, SyntaxTrivia other) {
                return trivia.RawKind == other.RawKind
                    && AnnotationsEqual(trivia.GetAnnotations(), other.GetAnnotations());
            }

            private bool AnnotationsEqual(IEnumerable<SyntaxAnnotation> annotations, IEnumerable<SyntaxAnnotation> other) {
                var annotationsEnumerator = annotations.GetEnumerator();
                var otherEnumerator = other.GetEnumerator();
                return false;
            }
        }
    }
}
