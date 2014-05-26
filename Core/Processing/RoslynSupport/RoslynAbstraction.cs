using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using AshMind.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    // this provides a way to manage Roslyn API changes between branches
    [ThreadSafe]
    public class RoslynAbstraction : IRoslynAbstraction {
        private readonly ConcurrentDictionary<string, Delegate> _delegateCache = new ConcurrentDictionary<string, Delegate>();
        
        [ThreadSafe]
        private static class CachedEnum<[ThreadSafe] TEnum> {
            public static readonly TEnum MaxValue = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Max();
        }

        public SyntaxTree ParseText<TSyntaxTree>(string code, ParseOptions options) {
            return GetDelegate<Func<string, ParseOptions, SyntaxTree>>(typeof(TSyntaxTree), "ParseText")
                        .Invoke(code, options);
        }

        public MetadataFileReference NewMetadataFileReference(string path) {
            return GetFactory<Func<string, MetadataFileReference>>().Invoke(path);
        }

        public TCompilationOptions NewCompilationOptions<TCompilationOptions>(OutputKind outputKind) {
            return GetFactory<Func<OutputKind, TCompilationOptions>>().Invoke(outputKind);
        }

        public TLanguageVersion GetMaxValue<TLanguageVersion>() {
            return CachedEnum<TLanguageVersion>.MaxValue;
        }
        
        private TDelegate GetFactory<TDelegate>([CallerMemberName, NotNull] string key = null) {
            // ReSharper disable once AssignNullToNotNullAttribute
            return (TDelegate)(object)_delegateCache.GetOrAdd(key, _ => (Delegate)(object)BuildFactory<TDelegate>());
        }

        private TDelegate GetDelegate<TDelegate>(Type type, string methodName, [CallerMemberName, NotNull] string key = null) {
            return (TDelegate)(object)_delegateCache.GetOrAdd(key, _ => (Delegate)(object)BuildDelegate<TDelegate>(type, methodName));
        }

        private TDelegate BuildDelegate<TDelegate>(Type type, string methodName) {
            var signature = GetParametersAndReturnType<TDelegate>();
            var parameters = signature.Item1;

            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                              .Where(m => m.Name == methodName);
            var resolved = ResolveMethod(methods, parameters);
            if (resolved == null)
                throw new Exception("Failed to find matching method on " + type + ".");

            return Expression.Lambda<TDelegate>(Expression.Call(resolved.Item1, resolved.Item2), parameters)
                             .Compile();
        }

        private TDelegate BuildFactory<TDelegate>() {
            var signature = GetParametersAndReturnType<TDelegate>();
            var parameters = signature.Item1;
            var returnType = signature.Item2;

            var constructors = signature.Item2.GetConstructors();
            var resolved = ResolveMethod(constructors, parameters);
            if (resolved == null)
                throw new Exception("Failed to find matching constructor on " + returnType + ".");

            return Expression.Lambda<TDelegate>(Expression.New(resolved.Item1, resolved.Item2), parameters)
                             .Compile();
        }

        private Tuple<ParameterExpression[], Type> GetParametersAndReturnType<TDelegate>() {
            if (typeof(TDelegate).FullName.SubstringBefore("`") != "System.Func")
                throw new NotSupportedException("Only Func<..> delegates are supported.");

            var argumentTypes = typeof(TDelegate).GetGenericArguments().ToList();
            var returnType = argumentTypes.Last();
            argumentTypes.RemoveAt(argumentTypes.Count - 1);
            var lambdaParameters = argumentTypes.Select(Expression.Parameter).ToArray();
            return Tuple.Create(lambdaParameters, returnType);
        }

        private Tuple<TMethod, Expression[]> ResolveMethod<TMethod>(IEnumerable<TMethod> methods, ParameterExpression[] lambdaParameters)
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