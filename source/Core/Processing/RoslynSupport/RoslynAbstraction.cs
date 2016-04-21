using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AshMind.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    // this provides a way to manage Roslyn API changes between branches
    [ThreadSafe]
    public class RoslynAbstraction : IRoslynAbstraction {
        #region ResolutionResult class

        private class ResolutionResult<TMethod> 
            where TMethod : MethodBase
        {
            public ResolutionResult([CanBeNull] Expression instance, [NotNull] TMethod method, [NotNull] Expression[] arguments) {
                Instance = instance;
                Method = method;
                Arguments = arguments;
            }

            [CanBeNull] public Expression Instance    { get; private set; }
            [NotNull]   public TMethod Method         { get; private set; }
            [NotNull]   public Expression[] Arguments { get; private set; }
        }

        #endregion

        private readonly ConcurrentDictionary<string, Delegate> _delegateCache = new ConcurrentDictionary<string, Delegate>();

        public EmitResult Emit(Compilation compilation, Stream stream) {
            return GetDelegate<Func<Compilation, Stream, EmitResult>>("Compilation.Emit", typeof(Compilation), "Emit").Invoke(compilation, stream);
        }

        private TDelegate GetDelegate<TDelegate>(string key, Type type, string methodName) {
            return (TDelegate)(object)_delegateCache.GetOrAdd(key, _ => (Delegate)(object)BuildDelegate<TDelegate>(type, methodName));
        }

        private TDelegate BuildDelegate<TDelegate>(Type type, string methodName) {
            var signature = GetParametersAndReturnType<TDelegate>();
            var parameters = signature.Item1;

            var methods = type.GetMethods()
                              .Where(m => m.IsPublic)
                              .Where(m => m.Name == methodName);
            var resolved = ResolveMethod(type, methods, parameters);
            
            return Expression.Lambda<TDelegate>(Expression.Call(resolved.Instance, resolved.Method, resolved.Arguments), parameters)
                             .Compile();
        }

        private Tuple<ParameterExpression[], Type> GetParametersAndReturnType<TDelegate>() {
            if (typeof(TDelegate).FullName.SubstringBefore("`") != "System.Func")
                throw new NotSupportedException("Only Func<..> delegates are supported.");

            var argumentTypes = typeof(TDelegate).GetGenericArguments().ToList();
            var returnType = argumentTypes.Last();
            argumentTypes.RemoveAt(argumentTypes.Count - 1);
            var lambdaParameters = argumentTypes.Select((t, i) => Expression.Parameter(t, "p" + (i+1))).ToArray();
            return Tuple.Create(lambdaParameters, returnType);
        }

        [NotNull]
        private ResolutionResult<TMethod> ResolveMethod<TMethod>(Type declaringType, IEnumerable<TMethod> methods, Expression[] potentialArguments)
            where TMethod : MethodBase
        {
            var report = new StringWriter();
            foreach (var method in methods) {
                report.WriteLine();
                report.WriteLine("Method: {0}", method);

                var instance = (Expression)null;
                var potentialArgumentsExceptInstance = potentialArguments.AsEnumerable();
                if (!method.IsStatic && !method.IsConstructor) {
                    instance = potentialArguments[0];
                    potentialArgumentsExceptInstance = potentialArguments.Skip(1);
                }

                var unusedPotentialArguments = potentialArgumentsExceptInstance.ToSet();

                var parameters = method.GetParameters();
                var arguments = new Expression[parameters.Length];
                var failed = false;
                for (var i = 0; i < parameters.Length; i++) {
                    var argument = ResolveArgument(parameters[i], unusedPotentialArguments);
                    if (argument == null) {
                        report.WriteLine("    {0}: could not resolve", parameters[i]);
                        failed = true;
                        break;
                    }

                    arguments[i] = argument;
                    report.WriteLine("    {0}: {1}", parameters[i], argument);
                }

                if (unusedPotentialArguments.Count > 0) {
                    report.WriteLine("    unused: {0}.", string.Join(", ", unusedPotentialArguments));
                    continue;
                }

                if (!failed)
                    return new ResolutionResult<TMethod>(instance, method, arguments);
            }

            throw new Exception("Failed to find matching method on " + declaringType + ":" + Environment.NewLine + report);
        }

        private static Expression ResolveArgument(ParameterInfo parameter, ISet<Expression> unusedPotentialArguments) {
            var argument = unusedPotentialArguments.SingleOrDefault(p => parameter.ParameterType.IsAssignableFrom(p.Type));
            if (argument != null) {
                unusedPotentialArguments.Remove(argument);
                return argument;
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