using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualBasic.CompilerServices;
using MirrorSharp;
using MirrorSharp.Advanced;
using SharpLab.Runtime;
using SharpLab.Server.Compilation.Internal;

namespace SharpLab.Server.Common.Languages {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class VisualBasicAdapter : ILanguageAdapter {
        private readonly IMetadataReferenceCollector _referenceCollector;
        private readonly IFeatureDiscovery _featureDiscovery;

        private static readonly ImmutableArray<KeyValuePair<string, object>> DebugPreprocessorSymbols = ImmutableArray.Create(new KeyValuePair<string,object>("DEBUG", true));
        private static readonly ImmutableArray<KeyValuePair<string, object>> ReleasePreprocessorSymbols = ImmutableArray<KeyValuePair<string, object>>.Empty;

        public VisualBasicAdapter(IMetadataReferenceCollector referenceCollector, IFeatureDiscovery featureDiscovery) {
            _referenceCollector = referenceCollector;
            _featureDiscovery = featureDiscovery;
        }

        public string LanguageName => LanguageNames.VisualBasic;

        public void SlowSetup(MirrorSharpOptions options) {
            // ReSharper disable HeapView.ObjectAllocation.Evident
            // ReSharper disable HeapView.DelegateAllocation
            options.EnableVisualBasic(o => {
                // This setup will only run if the language is used, so branches
                // where no one ever used VB will be faster to open.
                var maxLanguageVersion = Enum.GetValues(typeof(LanguageVersion)).Cast<LanguageVersion>().Max();
                var features = _featureDiscovery.SlowDiscoverAll().ToDictionary(f => f, f => (string) null);

                o.ParseOptions = new VisualBasicParseOptions(
                    maxLanguageVersion,
                    documentationMode: DocumentationMode.Diagnose
                ).WithFeatures(features);
                o.MetadataReferences = _referenceCollector.SlowGetMetadataReferencesRecursive(
                    typeof(StandardModuleAttribute).Assembly,
                    NetFrameworkRuntime.AssemblyOfValueTuple,
                    typeof(JitGenericAttribute).Assembly
                ).ToImmutableList();
            });
            // ReSharper restore HeapView.DelegateAllocation
            // ReSharper restore HeapView.ObjectAllocation.Evident
        }

        public void SetOptimize([NotNull] IWorkSession session, [NotNull] string optimize) {
            var project = session.Roslyn.Project;
            var parseOptions = ((VisualBasicParseOptions)project.ParseOptions);
            var compilationOptions = ((VisualBasicCompilationOptions)project.CompilationOptions);
            session.Roslyn.Project = project
                .WithParseOptions(parseOptions.WithPreprocessorSymbols(optimize == Optimize.Debug ? DebugPreprocessorSymbols : ReleasePreprocessorSymbols))
                .WithCompilationOptions(compilationOptions.WithOptimizationLevel(optimize == Optimize.Debug ? OptimizationLevel.Debug : OptimizationLevel.Release));
        }

        public void SetOptionsForTarget([NotNull] IWorkSession session, [NotNull] string target) {
            var outputKind = target != TargetNames.Run ? OutputKind.DynamicallyLinkedLibrary : OutputKind.ConsoleApplication;

            var project = session.Roslyn.Project;
            var options = ((VisualBasicCompilationOptions)project.CompilationOptions);
            session.Roslyn.Project = project.WithCompilationOptions(options.WithOutputKind(outputKind));
        }

        public ImmutableArray<int> GetMethodParameterLines(IWorkSession session, int lineInMethod, int columnInMethod) {
            var method = RoslynAdapterHelper.FindSyntaxNodeInSession(session, lineInMethod, columnInMethod)
                ?.AncestorsAndSelf()
                .OfType<MethodBlockBaseSyntax>()
                .FirstOrDefault();

            if (method == null)
                return ImmutableArray<int>.Empty;

            var parameters = method.BlockStatement.ParameterList.Parameters;
            var results = new int[parameters.Count];
            for (int i = 0; i < parameters.Count; i++) {
                results[i] = parameters[i].GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            }
            return ImmutableArray.Create(results);
        }
    }
}
