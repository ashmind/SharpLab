using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MirrorSharp;
using MirrorSharp.Advanced;
using SharpLab.Runtime;
using SharpLab.Server.Compilation.Internal;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace SharpLab.Server.MirrorSharp.Internal.Languages {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class CSharpIntegration : ILanguageIntegration {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof (LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();
        private static readonly IReadOnlyCollection<string> PreprocessorSymbols = new[] { "__DEMO_EXPERIMENTAL__" };
        
        private readonly ImmutableList<MetadataReference> _references;
        private readonly IReadOnlyDictionary<string, string> _features;

        public CSharpIntegration(IMetadataReferenceCollector referenceCollector, IFeatureDiscovery featureDiscovery) {
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

            options.CSharp.ParseOptions = new CSharpParseOptions(MaxLanguageVersion, preprocessorSymbols: PreprocessorSymbols).WithFeatures(_features);
            options.CSharp.CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            options.CSharp.MetadataReferences = _references;

            // ReSharper restore HeapView.ObjectAllocation.Evident
        }

        public void SetOptimize([NotNull] IWorkSession session, [NotNull] string optimize) {
            var project = session.Roslyn.Project;
            var options = ((CSharpCompilationOptions)project.CompilationOptions);
            session.Roslyn.Project = project.WithCompilationOptions(
                options.WithOptimizationLevel(optimize == Optimize.Debug ? OptimizationLevel.Debug : OptimizationLevel.Release)
            );
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
    }
}
