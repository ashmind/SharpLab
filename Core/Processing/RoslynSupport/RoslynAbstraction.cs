using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AshMind.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    // this provides a way to manage Roslyn API changes between branches
    [ThreadSafe]
    public class RoslynAbstraction : IRoslynAbstraction {
        [ThreadSafe]
        private static class Cached<[ThreadSafe] TDelegate> {
            private static readonly Expression<TDelegate> FactoryExpression = BuildFactory<TDelegate>();
            public static readonly TDelegate Factory = FactoryExpression.Compile();
        }

        private readonly Lazy<LanguageVersion> _maxLanguageVersion = new Lazy<LanguageVersion>(
            () => Enum.GetValues(typeof(LanguageVersion)).Cast<LanguageVersion>().Max()
        );

        public MetadataFileReference NewMetadataFileReference(string path) {
            return Cached<Func<string, MetadataFileReference>>.Factory(path);
        }

        public CSharpCompilationOptions NewCSharpCompilationOptions(OutputKind outputKind) {
            return Cached<Func<OutputKind, CSharpCompilationOptions>>.Factory(outputKind);
        }

        public LanguageVersion GetMaxLanguageVersion() {
            return _maxLanguageVersion.Value;
        }

        private static Expression<TDelegate> BuildFactory<TDelegate>() {
            if (typeof(TDelegate).FullName.SubstringBefore("`") != "System.Func")
                throw new NotSupportedException("Only Func<..> delegates are supported.");

            var argumentTypes = typeof(TDelegate).GetGenericArguments().ToList();
            var returnType = argumentTypes.Last();
            argumentTypes.RemoveAt(argumentTypes.Count - 1);
            var lambdaParameters = argumentTypes.Select(Expression.Parameter).ToArray();
            
            var constructors = returnType.GetConstructors();
            var resolved = ResolveMethod(constructors, lambdaParameters);
            if (resolved == null)
                throw new Exception("Failed to find matching constructor on " + returnType + ".");

            return Expression.Lambda<TDelegate>(
                Expression.New(resolved.Item1, resolved.Item2), lambdaParameters
            );
        }

        private static Tuple<TMethod, Expression[]> ResolveMethod<TMethod>(IEnumerable<TMethod> methods, ParameterExpression[] lambdaParameters)
            where TMethod : MethodBase
        {
            foreach (var method in methods) {
                var unusedLambdaParameters = lambdaParameters.ToDictionary(p => p.Type);
                var parameters = method.GetParameters();
                var arguments = new Expression[parameters.Length];
                var failed = false;
                for (var i = 0; i < parameters.Length; i++) {
                    var argument = ResolveArgument(parameters[i], unusedLambdaParameters);
                    if (argument == null) {
                        failed = true;
                        break;
                    }

                    arguments[i] = argument;
                }

                if (!failed)
                    return Tuple.Create(method, arguments);
            }

            return null;
        }

        private static Expression ResolveArgument(ParameterInfo parameter, IDictionary<Type, ParameterExpression> unusedLambdaParameters) {
            var lambdaParameter = unusedLambdaParameters.GetValueOrDefault(parameter.ParameterType);
            if (lambdaParameter != null) {
                unusedLambdaParameters.Remove(parameter.ParameterType);
                return lambdaParameter;
            }

            if (parameter.HasDefaultValue) {
                var value = parameter.DefaultValue;
                if (value == null && parameter.ParameterType.IsValueType && !parameter.ParameterType.IsGenericTypeDefinedAs(typeof(Nullable<>)))
                    value = Activator.CreateInstance(parameter.ParameterType);

                return Expression.Constant(value, parameter.ParameterType);
            }

            return null;
        }
    }
}