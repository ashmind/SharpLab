using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MirrorSharp;
using MirrorSharp.Advanced;
using SharpLab.Runtime;
using SharpLab.Server.Compilation.Internal;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace SharpLab.Server.Common.Languages {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class CSharpAdapter : ILanguageAdapter {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof (LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();
        private static readonly ImmutableArray<string> ReleasePreprocessorSymbols = ImmutableArray.Create("__DEMO_EXPERIMENTAL__");
        private static readonly ImmutableArray<string> DebugPreprocessorSymbols = ReleasePreprocessorSymbols.Add("DEBUG");
        
        private readonly ImmutableList<MetadataReference> _references;
        private readonly IReadOnlyDictionary<string, string> _features;

        public CSharpAdapter(IMetadataReferenceCollector referenceCollector, IFeatureDiscovery featureDiscovery) {
            _references = referenceCollector.SlowGetMetadataReferencesRecursive(
                // Essential
                typeof(Binder).Assembly,
                NetFrameworkRuntime.AssemblyOfValueTuple,

                // Runtime
                typeof(JitGenericAttribute).Assembly,

                // Requested
                typeof(IDataReader).Assembly // System.Data
            ).ToImmutableList();
            _features = featureDiscovery.SlowDiscoverAll().ToDictionary(f => f, f => (string)null);
        }

        public string LanguageName => LanguageNames.CSharp;

        public void SlowSetup(MirrorSharpOptions options) {
            // ReSharper disable HeapView.ObjectAllocation.Evident

            options.CSharp.ParseOptions = new CSharpParseOptions(MaxLanguageVersion, preprocessorSymbols: DebugPreprocessorSymbols).WithFeatures(_features);
            options.CSharp.CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            options.CSharp.MetadataReferences = _references;

            // ReSharper restore HeapView.ObjectAllocation.Evident
        }

        public void SetOptimize([NotNull] IWorkSession session, [NotNull] string optimize) {
            var project = session.Roslyn.Project;
            var parseOptions = ((CSharpParseOptions)project.ParseOptions);
            var compilationOptions = ((CSharpCompilationOptions)project.CompilationOptions);
            session.Roslyn.Project = project
                .WithParseOptions(parseOptions.WithPreprocessorSymbols(optimize == Optimize.Debug ? DebugPreprocessorSymbols : ReleasePreprocessorSymbols))
                .WithCompilationOptions(compilationOptions.WithOptimizationLevel(optimize == Optimize.Debug ? OptimizationLevel.Debug : OptimizationLevel.Release));
        }

        public void SetOptionsForTarget(IWorkSession session, string target) {
            var outputKind = target != TargetNames.Run ? OutputKind.DynamicallyLinkedLibrary : OutputKind.ConsoleApplication;
            var allowUnsafe = target != TargetNames.Run;

            var project = session.Roslyn.Project;
            var options = ((CSharpCompilationOptions)project.CompilationOptions);
            session.Roslyn.Project = project.WithCompilationOptions(
                options.WithOutputKind(outputKind).WithAllowUnsafe(allowUnsafe)
            );
        }

        public int? GetMethodStartLine(IWorkSession session, int lineInMethod, int columnInMethod) {
            var declaration = RoslynAdapterHelper.FindSyntaxNodeInSession(session, lineInMethod, columnInMethod)
                ?.Ancestors()
                .OfType<MemberDeclarationSyntax>()
                .FirstOrDefault();
            if (!(declaration is BaseMethodDeclarationSyntax method))
                return null;
            return method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        }
    }
}
