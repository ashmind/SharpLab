using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AshMind.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    // this provides a way to manage Roslyn API changes between branches
    public class RoslynAbstraction : IRoslynAbstraction {
        private static class Cached<TDelegate> {
            public static readonly TDelegate Factory = BuildFactory<TDelegate>();
        }

        public MetadataFileReference NewMetadataFileReference(string path) {
            return Cached<Func<string, MetadataFileReference>>.Factory(path);
        }

        public CSharpCompilationOptions NewCSharpCompilationOptions(OutputKind outputKind) {
            return Cached<Func<OutputKind, CSharpCompilationOptions>>.Factory(outputKind);
        }
        
        private static TDelegate BuildFactory<TDelegate>() {
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
            ).Compile();
        }

        private static Tuple<TMethod, Expression[]> ResolveMethod<TMethod>(IEnumerable<TMethod> methods, ParameterExpression[] lambdaParameters)
            where TMethod : MethodBase
        {
            foreach (var method in methods) {
                var parameters = method.GetParameters();
                var arguments = new Expression[parameters.Length];
                var failed = false;
                for (var i = 0; i < parameters.Length; i++) {
                    var argument = ResolveArgument(parameters[i], lambdaParameters);
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

        private static Expression ResolveArgument(ParameterInfo parameter, ParameterExpression[] lambdaParameters) {
            var lambdaParameter = lambdaParameters.SingleOrDefault(p => p.Type == parameter.ParameterType);
            if (lambdaParameter != null)
                return lambdaParameter;

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