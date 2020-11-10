using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FSharp.Collections;
using FSharp.Compiler;
using MirrorSharp.Advanced;
using MirrorSharp.FSharp.Advanced;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation.AstOnly {
    using Range = FSharp.Compiler.Range;

    public class FSharpAstTarget : IAstTarget {
        private delegate void SerializeChildAction<T>(T item, IFastJsonWriter writer, string parentPropertyName, ref bool childrenStarted, IFSharpSession session);
        private delegate void SerializeChildrenAction(object parent, IFastJsonWriter writer, ref bool childrenStarted, IFSharpSession session);
        private delegate Range.range GetRangeFunc(object target);

        private static readonly ConcurrentDictionary<Type, Lazy<SerializeChildrenAction>> ChildrenSerializers =
            new ConcurrentDictionary<Type, Lazy<SerializeChildrenAction>>();
        private static readonly ConcurrentDictionary<Type, Lazy<GetRangeFunc?>> RangeGetters =
            new ConcurrentDictionary<Type, Lazy<GetRangeFunc?>>();
        private static readonly Lazy<IReadOnlyDictionary<Type, Func<object, string>>> TagNameGetters =
            new Lazy<IReadOnlyDictionary<Type, Func<object, string>>>(CompileTagNameGetters, LazyThreadSafetyMode.ExecutionAndPublication);
        private static readonly Lazy<IReadOnlyDictionary<Type, Func<Ast.SynConst, string>>> ConstValueGetters =
            new Lazy<IReadOnlyDictionary<Type, Func<Ast.SynConst, string>>>(CompileConstValueGetters, LazyThreadSafetyMode.ExecutionAndPublication);
        private static readonly Lazy<IReadOnlyDictionary<Type, string>> AstTypeNames =
            new Lazy<IReadOnlyDictionary<Type, string>>(CollectAstTypeNames, LazyThreadSafetyMode.ExecutionAndPublication);

        private static class Methods {
            // ReSharper disable MemberHidesStaticFromOuterClass
            // ReSharper disable HeapView.DelegateAllocation
            public static readonly MethodInfo SerializeNode =
                ((SerializeChildAction<object>)FSharpAstTarget.SerializeNode).Method;
            public static readonly MethodInfo SerializeList =
                ((SerializeChildAction<FSharpList<object>>)FSharpAstTarget.SerializeList).Method.GetGenericMethodDefinition();
            public static readonly MethodInfo SerializeIdent =
                ((SerializeChildAction<Ast.Ident>)FSharpAstTarget.SerializeIdent).Method;
            public static readonly MethodInfo SerializeIdentList =
                ((SerializeChildAction<FSharpList<Ast.Ident>>)FSharpAstTarget.SerializeIdentList).Method;
            public static readonly MethodInfo SerializeEnum =
                ((SerializeChildAction<int>)FSharpAstTarget.SerializeEnum).Method.GetGenericMethodDefinition();
            // ReSharper restore HeapView.DelegateAllocation
            // ReSharper restore MemberHidesStaticFromOuterClass
        }

        private static class EnumCache<TEnum>
            where TEnum : struct, IFormattable
        {
            public static readonly IReadOnlyDictionary<TEnum, string> Strings = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToDictionary(e => e, e => e.ToString("G", null));
        }

        public Task<object> GetAstAsync(IWorkSession session, CancellationToken cancellationToken) {
            var parseResult = session.FSharp().GetLastParseResults();
            if (parseResult == null)
                throw new InvalidOperationException("Current session does not include F# parse results yet.");
            return Task.FromResult((object)parseResult.ParseTree.Value);
        }

        public void SerializeAst(object ast, IFastJsonWriter writer, IWorkSession session) {
            var root = ((Ast.ParsedInput.ImplFile)ast).Item;
            writer.WriteStartArray();
            var childrenStarted = true;
            SerializeNode(root, writer, null, ref childrenStarted, session.FSharp());
            writer.WriteEndArray();
        }

        private static void SerializeNode(object node, IFastJsonWriter writer, string? parentPropertyName, ref bool parentChildrenStarted, IFSharpSession session) {
            EnsureChildrenStarted(ref parentChildrenStarted, writer);
            writer.WriteStartObject();
            writer.WriteProperty("kind", AstTypeNames.Value[node.GetType()]);
            if (parentPropertyName != null)
                writer.WriteProperty("property", parentPropertyName);

            if (node is Ast.SynConst @const) {
                writer.WriteProperty("type", "token");
                if (@const is Ast.SynConst.String @string) {
                    writer.WritePropertyName("value");
                    writer.WriteValueFromParts("\"", @string.text, "\"");
                }
                else if (@const is Ast.SynConst.Char @char) {
                    writer.WritePropertyName("value");
                    writer.WriteValueFromParts("'", @char.Item, "'");
                }
                else {
                    if (ConstValueGetters.Value.TryGetValue(@const.GetType(), out var getter)) {
                        writer.WritePropertyName("value");
                        writer.WriteValue(getter(@const));
                    }
                }
            }
            else {
                writer.WriteProperty("type", "node");
                var tagName = GetTagName(node);
                if (tagName != null)
                    writer.WriteProperty("value", tagName);
            }
            var rangeGetter = GetRangeGetter(node.GetType());
            if (rangeGetter != null)
                SerializeRangeProperty(rangeGetter(node), writer, session);

            var childrenStarted = false;
            GetChildrenSerializer(node.GetType()).Invoke(node, writer, ref childrenStarted, session);
            EnsureChildrenEnded(childrenStarted, writer);
            writer.WriteEndObject();
        }

        private static void SerializeList<T>(FSharpList<T> list, IFastJsonWriter writer, string? parentPropertyName, ref bool parentChildrenStarted, IFSharpSession session) {
            foreach (var item in list) {
                SerializeNode(item!, writer, null /* UI does not support list property names at the moment */, ref parentChildrenStarted, session);
            }
        }

        private static void SerializeIdent(Ast.Ident ident, IFastJsonWriter writer, string? parentPropertyName, ref bool parentChildrenStarted, IFSharpSession session) {
            EnsureChildrenStarted(ref parentChildrenStarted, writer);
            writer.WriteStartObject();
            writer.WriteProperty("type", "token");
            writer.WriteProperty("kind", "Ast.Ident");
            if (parentPropertyName != null)
                writer.WriteProperty("property", parentPropertyName);
            writer.WriteProperty("value", ident.idText);
            SerializeRangeProperty(ident.idRange, writer, session);
            writer.WriteEndObject();
        }

        private static void SerializeIdentList(FSharpList<Ast.Ident> list, IFastJsonWriter writer, string? parentPropertyName, ref bool parentChildrenStarted, IFSharpSession session) {
            foreach (var ident in list) {
                SerializeIdent(ident, writer, parentPropertyName, ref parentChildrenStarted, session);
            }
        }

        private static void SerializeEnum<TEnum>(TEnum value, IFastJsonWriter writer, string? parentPropertyName, ref bool parentChildrenStarted, IFSharpSession session)
            where TEnum: struct, IFormattable
        {
            EnsureChildrenStarted(ref parentChildrenStarted, writer);
            if (parentPropertyName != null) {
                writer.WriteStartObject();
                writer.WriteProperty("type", "value");
                writer.WriteProperty("property", parentPropertyName);
                writer.WriteProperty("value", EnumCache<TEnum>.Strings[value]);
                writer.WriteEndObject();
            }
            else {
                writer.WriteValue(EnumCache<TEnum>.Strings[value]);
            }
        }

        private static void SerializeRangeProperty(Range.range range, IFastJsonWriter writer, IFSharpSession session) {
            writer.WritePropertyName("range");
            var startOffset = session.ConvertToOffset(range.StartLine, range.StartColumn);
            var endOffset = session.ConvertToOffset(range.EndLine, range.EndColumn);
            writer.WriteValueFromParts(startOffset, '-', endOffset);
        }

        private static void EnsureChildrenStarted(ref bool childrenStarted, IFastJsonWriter writer) {
            if (childrenStarted)
                return;
            writer.WritePropertyStartArray("children");
            childrenStarted = true;
        }

        private static void EnsureChildrenEnded(bool childrenStarted, IFastJsonWriter writer) {
            if (!childrenStarted)
                return;
            writer.WriteEndArray();
        }

        private static SerializeChildrenAction GetChildrenSerializer(Type type) {
            return ChildrenSerializers.GetOrAdd(
                type,
                t => new Lazy<SerializeChildrenAction>(() => CompileChildrenSerializer(t), LazyThreadSafetyMode.ExecutionAndPublication)
            ).Value;
        }

        private static SerializeChildrenAction CompileChildrenSerializer(Type type) {
            var nodeAsObject = Expression.Parameter(typeof(object));
            var writer = Expression.Parameter(typeof(IFastJsonWriter));
            var refChildrenStarted = Expression.Parameter(typeof(bool).MakeByRefType());
            var session = Expression.Parameter(typeof(IFSharpSession));

            var node = Expression.Variable(type);
            var body = new List<Expression> {
                Expression.Assign(node, Expression.Convert(nodeAsObject, type))
            };

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                if (ShouldSkipNodeProperty(type, property))
                    continue;
                var propertyType = property.PropertyType;
                var method = GetMethodToSerialize(propertyType);
                if (method == null)
                    continue;

                var propertyName = property.Name;
                if (Regex.IsMatch(propertyName, @"^Item\d*$"))
                    propertyName = null;
                body.Add(Expression.Call(method, Expression.Property(node, property), writer, Expression.Constant(propertyName, typeof(string)), refChildrenStarted, session));
            }

            return Expression.Lambda<SerializeChildrenAction>(
                Expression.Block(new[] {node}, body),
                nodeAsObject, writer, refChildrenStarted, session
            ).Compile();
        }

        private static MethodInfo? GetMethodToSerialize(Type propertyType) {
            if (propertyType == typeof(Ast.Ident))
                return Methods.SerializeIdent;

            if (propertyType == typeof(FSharpList<Ast.Ident>))
                return Methods.SerializeIdentList;

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(FSharpList<>)) {
                var elementType = propertyType.GetGenericArguments()[0];
                if (!IsNodeType(elementType))
                    return null;
                return Methods.SerializeList.MakeGenericMethod(elementType);
            }

            if (!IsNodeType(propertyType))
                return null;

            if (propertyType.IsEnum)
                return Methods.SerializeEnum.MakeGenericMethod(propertyType);

            return Methods.SerializeNode;
        }

        private static GetRangeFunc? GetRangeGetter(Type type) {
            return RangeGetters.GetOrAdd(
                type,
                t => new Lazy<GetRangeFunc?>(() => CompileRangeGetter(t), LazyThreadSafetyMode.ExecutionAndPublication)
            ).Value;
        }

        private static GetRangeFunc? CompileRangeGetter(Type type) {
            var rangeProperty = type.GetProperty("Range");
            if (rangeProperty == null)
                return null;

            var nodeAsObject = Expression.Parameter(typeof(object));
            var body = Expression.Property(Expression.Convert(nodeAsObject, type), rangeProperty);

            return Expression.Lambda<GetRangeFunc>(body, new[] { nodeAsObject }).Compile();
        }

        private static bool ShouldSkipNodeProperty(Type type, PropertyInfo property) {
            return (type == typeof(Ast.LongIdentWithDots) && property.Name == nameof(Ast.LongIdentWithDots.id));
        }

        private static bool IsNodeType(Type type) {
            return type.DeclaringType == typeof(Ast)
                && type != typeof(Ast.QualifiedNameOfFile)
                && type != typeof(Ast.XmlDocCollector)
                && type != typeof(Ast.PreXmlDoc)
                && type != typeof(Ast.SynModuleOrNamespaceKind)
                && !(type.Name.StartsWith("SequencePoint"));
        }

        private static string? GetTagName(object node) {
            return TagNameGetters.Value.TryGetValue(node.GetType(), out var getter)
                 ? getter.Invoke(node)
                 : null;
        }

        private static IReadOnlyDictionary<Type, Func<object, string>> CompileTagNameGetters() {
            var getters = new Dictionary<Type, Func<object, string>>();
            CompileAndCollectTagNameGettersRecursive(getters, typeof(Ast));
            return getters;
        }

        private static void CompileAndCollectTagNameGettersRecursive(IDictionary<Type, Func<object, string>> getters, Type astType) {
            foreach (var nested in astType.GetNestedTypes()) {
                if (nested.Name == "Tags") {
                    getters.Add(astType, CompileTagNameGetter(astType, nested));
                    continue;
                }
                CompileAndCollectTagNameGettersRecursive(getters, nested);
            }
        }

        private static Func<object, string> CompileTagNameGetter(Type astType, Type tagsType) {
            var tagMap = tagsType
                .GetFields()
                .OrderBy(f => (int)f.GetValue(null)!)
                .Select(f => f.Name)
                .ToArray();
            var nodeUntyped = Expression.Parameter(typeof(object));
            var tagGetter = Expression.Lambda<Func<object, int>>(
                Expression.Property(Expression.Convert(nodeUntyped, astType), "Tag"),
                nodeUntyped
            ).Compile();
            return instance => tagMap[tagGetter(instance)];
        }

        private static IReadOnlyDictionary<Type, Func<Ast.SynConst, string>> CompileConstValueGetters() {
            var getters = new Dictionary<Type, Func<Ast.SynConst, string>>();
            foreach (var type in typeof(Ast.SynConst).GetNestedTypes()) {
                if (type.BaseType != typeof(Ast.SynConst))
                    continue;

                var valueProperty = type.GetProperty("Item");
                if (valueProperty == null)
                    continue;

                // We assume that FSharp discriminated union members always have their ToString method overriden.
                var toString = valueProperty.PropertyType.GetMethod("ToString", Type.EmptyTypes)!;
                var constUntyped = Expression.Parameter(typeof(Ast.SynConst));
                getters.Add(type, Expression.Lambda<Func<Ast.SynConst, string>>(
                    Expression.Call(Expression.Property(Expression.Convert(constUntyped, type), valueProperty), toString),
                    constUntyped
                ).Compile());
            }
            return getters;
        }

        private static IReadOnlyDictionary<Type, string> CollectAstTypeNames() {
            var results = new Dictionary<Type, string>();
            void CollectRecusive(Type astType, string parentPrefix) {
                var name = parentPrefix + astType.Name;
                var prefix = name + ".";
                results.Add(astType, name);
                foreach (var nested in astType.GetNestedTypes()) {
                    CollectRecusive(nested, prefix);
                }
            }

            CollectRecusive(typeof(Ast), "");
            return results;
        }

        public IReadOnlyCollection<string> SupportedLanguageNames { get; } = new[] {"F#"};
    }
}