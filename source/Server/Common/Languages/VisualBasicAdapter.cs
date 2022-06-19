using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using MirrorSharp;
using MirrorSharp.Advanced;
using SharpLab.Server.Common.Internal;
using SharpLab.Server.Compilation.Internal;

namespace SharpLab.Server.Common.Languages {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class VisualBasicAdapter : ILanguageAdapter {
        private static readonly ImmutableArray<KeyValuePair<string, object>> DebugPreprocessorSymbols = PreprocessorSymbols.Debug.Select(s => KeyValuePair.Create(s, (object)true)).ToImmutableArray();
        private static readonly ImmutableArray<KeyValuePair<string, object>> ReleasePreprocessorSymbols = PreprocessorSymbols.Release.Select(s => KeyValuePair.Create(s, (object)true)).ToImmutableArray();

        private readonly IAssemblyPathCollector _assemblyPathCollector;
        private readonly AssemblyReferenceDiscoveryTaskSource _assemblyReferenceDiscoveryTaskSource = new();
        private readonly IAssemblyDocumentationResolver _documentationResolver;

        public VisualBasicAdapter(IAssemblyPathCollector assemblyPathCollector, IAssemblyDocumentationResolver documentationResolver) {
            _assemblyPathCollector = assemblyPathCollector;
            _documentationResolver = documentationResolver;
        }

        public string LanguageName => LanguageNames.VisualBasic;
        public AssemblyReferenceDiscoveryTask AssemblyReferenceDiscoveryTask => _assemblyReferenceDiscoveryTaskSource.Task;

        public void SlowSetup(MirrorSharpOptions options) {
            // ReSharper disable HeapView.ObjectAllocation.Evident
            // ReSharper disable HeapView.DelegateAllocation
            options.EnableVisualBasic(o => {
                // This setup will only run if the language is used, so branches
                // where no one ever used VB will be faster to open.
                var maxLanguageVersion = Enum.GetValues(typeof(LanguageVersion)).Cast<LanguageVersion>().Max();

                o.ParseOptions = new VisualBasicParseOptions(
                    maxLanguageVersion,
                    documentationMode: DocumentationMode.Diagnose
                );
                var referencedAssemblies = _assemblyPathCollector.SlowGetAllAssemblyPathsIncludingReferences(
                    // Essential
                    NetFrameworkRuntime.AssemblyOfValueTuple.GetName().Name!,
                    NetFrameworkRuntime.AssemblyOfValueTask.GetName().Name!,
                    NetFrameworkRuntime.AssemblyOfSpan.GetName().Name!,
                    "Microsoft.VisualBasic",

                    // Runtime
                    "SharpLab.Runtime",

                    // Requested
                    "System.Data",
                    "System.Runtime.CompilerServices.Unsafe",
                    "System.Runtime.Intrinsics",
                    "System.Web.HttpUtility",
                    "System.Xml.Linq"
                ).ToImmutableList();
                _assemblyReferenceDiscoveryTaskSource.Complete(referencedAssemblies);
                o.MetadataReferences = referencedAssemblies
                    .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path, documentation: _documentationResolver.GetDocumentation(path)))
                    .ToImmutableList();
            });
            // ReSharper restore HeapView.DelegateAllocation
            // ReSharper restore HeapView.ObjectAllocation.Evident
        }

        public void SetOptimize(IWorkSession session, string optimize) {
            var project = session.Roslyn.Project;
            var parseOptions = ((VisualBasicParseOptions)project.ParseOptions!); // not null since this class always sets it
            var compilationOptions = ((VisualBasicCompilationOptions)project.CompilationOptions!);
            session.Roslyn.Project = project
                .WithParseOptions(parseOptions.WithPreprocessorSymbols(optimize == Optimize.Debug ? DebugPreprocessorSymbols : ReleasePreprocessorSymbols))
                .WithCompilationOptions(compilationOptions.WithOptimizationLevel(optimize == Optimize.Debug ? OptimizationLevel.Debug : OptimizationLevel.Release));
        }

        public void SetOptionsForTarget(IWorkSession session, string target) {
            var outputKind = target is TargetNames.Run or TargetNames.RunIL
                ? OutputKind.ConsoleApplication
                : OutputKind.DynamicallyLinkedLibrary;

            var project = session.Roslyn.Project;
            var options = ((VisualBasicCompilationOptions)project.CompilationOptions!);
            session.Roslyn.Project = project.WithCompilationOptions(options.WithOutputKind(outputKind));
        }

        public ImmutableArray<int> GetMethodParameterLines(IWorkSession session, int lineInMethod, int columnInMethod) {
            var declaration = RoslynAdapterHelper.FindSyntaxNodeInSession(session, lineInMethod, columnInMethod)
                ?.AncestorsAndSelf()
                .FirstOrDefault(x => x is MethodBlockBaseSyntax || x is LambdaExpressionSyntax);

            var parameters = declaration switch
            {
                MethodBlockBaseSyntax m => m.BlockStatement.ParameterList.Parameters,
                LambdaExpressionSyntax l => l.SubOrFunctionHeader.ParameterList.Parameters,
                _ => SyntaxFactory.SeparatedList<ParameterSyntax>()
            };
            if (parameters.Count == 0)
                return ImmutableArray<int>.Empty;

            var results = new int[parameters.Count];
            for (var i = 0; i < parameters.Count; i++) {
                results[i] = parameters[i].GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            }
            return ImmutableArray.Create(results);
        }

        public ImmutableArray<string?> GetCallArgumentIdentifiers(IWorkSession session, int callStartLine, int callStartColumn) {
            var call = RoslynAdapterHelper.FindSyntaxNodeInSession(session, callStartLine, callStartColumn) as InvocationExpressionSyntax;
            if (call == null)
                return ImmutableArray<string?>.Empty;

            var arguments = call.ArgumentList.Arguments;
            if (arguments.Count == 0)
                return ImmutableArray<string?>.Empty;

            var results = new string?[arguments.Count];
            for (var i = 0; i < arguments.Count; i++) {
                results[i] = (arguments[i].GetExpression() is IdentifierNameSyntax n) ? n.Identifier.ValueText : null;
            }
            return ImmutableArray.Create(results);
        }
    }
}
