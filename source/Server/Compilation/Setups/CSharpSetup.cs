using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MirrorSharp;
using SharpLab.Runtime;
using SharpLab.Server.Compilation.Internal;
using SharpLab.Server.MirrorSharp;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace SharpLab.Server.Compilation.Setups {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class CSharpSetup : IMirrorSharpSetup {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof (LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();
        private static readonly IReadOnlyCollection<string> PreprocessorSymbols = new[] { "__DEMO_EXPERIMENTAL__" };
        
        private readonly ImmutableList<MetadataReference> _references;
        private readonly IReadOnlyDictionary<string, string> _features;

        public CSharpSetup(IMetadataReferenceCollector referenceCollector, IFeatureDiscovery featureDiscovery) {
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

        public void SlowApplyTo(MirrorSharpOptions options) {
            // ReSharper disable HeapView.ObjectAllocation.Evident

            options.CSharp.ParseOptions = new CSharpParseOptions(MaxLanguageVersion, preprocessorSymbols: PreprocessorSymbols).WithFeatures(_features);
            options.CSharp.CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
            options.CSharp.MetadataReferences = _references;

            // ReSharper restore HeapView.ObjectAllocation.Evident
        }
    }
}
