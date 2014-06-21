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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Delegate = System.Delegate;
using Expression = System.Linq.Expressions.Expression;

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
        
        [ThreadSafe]
        private static class CachedEnum<[ThreadSafe] TEnum> {
            public static readonly TEnum MaxValue = Enumerable.Cast<TEnum>(Enum.GetValues(typeof(TEnum))).Max();
        }

        public SyntaxTree ParseText<TParseOptions>(Type syntaxTreeType, string code, TParseOptions options)
            where TParseOptions: ParseOptions
        {
            return GetDelegate<Func<string, TParseOptions, SyntaxTree>>(syntaxTreeType.Name + ".ParseText", syntaxTreeType, "ParseText")
                        .Invoke(code, options);
        }

        public EmitResult Emit(Compilation compilation, Stream stream) {
            return GetDelegate<Func<Compilation, Stream, EmitResult>>("Compilation.Emit", typeof(Compilation), "Emit").Invoke(compilation, stream);
        }

        public MetadataReference MetadataReferenceFromPath(string path) {
            var factory = (Func<string, MetadataReference>)_delegateCache.GetOrAdd("MetadataReferenceFromPath", _ => BuildMetadataReferenceFromPath());
            return factory(path);
        }

        private Func<string, MetadataReference> BuildMetadataReferenceFromPath() {
            // latest master
            var imageReferenceType = typeof(MetadataReference).Assembly.GetType("Microsoft.CodeAnalysis.MetadataImageReference", false);
            if (imageReferenceType != null) {
                var imageReferenceFactory = BuildFactory<Func<Stream, MetadataReference>>(imageReferenceType);
                return path => {
                    using (var stream = File.OpenRead(path)) {
                        return imageReferenceFactory(stream);
                    }
                };
            }

            // older APIs
            var fileReferenceType = typeof(MetadataReference).Assembly.GetType("Microsoft.CodeAnalysis.MetadataFileReference", true);
            return BuildFactory<Func<string, MetadataReference>>(fileReferenceType);
        }

        public TParseOptions NewParseOptions<TLanguageVersion, TParseOptions>(TLanguageVersion languageVersion, SourceCodeKind kind) {
            return GetFactory<Func<TLanguageVersion, SourceCodeKind, TParseOptions>>(typeof(TParseOptions).Name + ".new").Invoke(languageVersion, kind);
        }

        public TCompilationOptions NewCompilationOptions<TCompilationOptions>(OutputKind outputKind) {
            return GetFactory<Func<OutputKind, TCompilationOptions>>(typeof(TCompilationOptions).Name + ".new").Invoke(outputKind);
        }

        public TLanguageVersion GetMaxValue<TLanguageVersion>() {
            return CachedEnum<TLanguageVersion>.MaxValue;
        }
        
        private TDelegate GetFactory<TDelegate>(string key) {
            return (TDelegate)(object)_delegateCache.GetOrAdd(key, _ => (Delegate)(object)BuildFactory<TDelegate>());
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

        private TDelegate BuildFactory<TDelegate>(Type type = null) {
            var signature = GetParametersAndReturnType<TDelegate>();
            var parameters = signature.Item1;
            var returnType = signature.Item2;
            var constructedType = type ?? returnType;

            var constructors = constructedType.GetConstructors().Where(c => c.IsPublic);
            var resolved = ResolveMethod(returnType, constructors, parameters);

            return Expression.Lambda<TDelegate>(Expression.New(resolved.Method, resolved.Arguments), parameters)
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
        private ResolutionResult<TMethod> ResolveMethod<TMethod>(Type declaringType, IEnumerable<TMethod> methods, ParameterExpression[] lambdaParameters)
            where TMethod : MethodBase
        {
            var report = new StringWriter();
            foreach (var method in methods) {
                report.WriteLine();
                report.WriteLine("Method: {0}", method);

                var instance = (Expression)null;
                var lambdaParametersExceptInstance = lambdaParameters.AsEnumerable();
                if (!method.IsStatic && !method.IsConstructor) {
                    instance = lambdaParameters[0];
                    lambdaParametersExceptInstance = lambdaParameters.Skip(1);
                }

                var unusedLambdaParameters = lambdaParametersExceptInstance.ToSet();

                var parameters = method.GetParameters();
                var arguments = new Expression[parameters.Length];
                var failed = false;
                for (var i = 0; i < parameters.Length; i++) {
                    var argument = ResolveArgument(parameters[i], unusedLambdaParameters);
                    if (argument == null) {
                        report.WriteLine("    {0}: could not resolve", parameters[i]);
                        failed = true;
                        break;
                    }

                    arguments[i] = argument;
                    report.WriteLine("    {0}: {1}", parameters[i], argument);
                }

                if (unusedLambdaParameters.Count > 0) {
                    report.WriteLine("    unused: {0}.", string.Join(", ", unusedLambdaParameters));
                    continue;
                }

                if (!failed)
                    return new ResolutionResult<TMethod>(instance, method, arguments);
            }

            throw new Exception("Failed to find matching method on " + declaringType + ":" + Environment.NewLine + report);
        }

        private static Expression ResolveArgument(ParameterInfo parameter, ISet<ParameterExpression> unusedLambdaParameters) {
            var lambdaParameter = unusedLambdaParameters.SingleOrDefault(p => parameter.ParameterType.IsAssignableFrom(p.Type));
            if (lambdaParameter != null) {
                unusedLambdaParameters.Remove(lambdaParameter);
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