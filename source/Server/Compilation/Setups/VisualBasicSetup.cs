using System;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using MirrorSharp;
using SharpLab.Runtime;
using SharpLab.Server.Compilation.Internal;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Compilation.Setups {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class VisualBasicSetup : IMirrorSharpSetup {
        private readonly IMetadataReferenceCollector _referenceCollector;
        private readonly IFeatureDiscovery _featureDiscovery;

        public VisualBasicSetup(IMetadataReferenceCollector referenceCollector, IFeatureDiscovery featureDiscovery) {
            _referenceCollector = referenceCollector;
            _featureDiscovery = featureDiscovery;
        }

        public void SlowApplyTo(MirrorSharpOptions options) {
            // ReSharper disable HeapView.ObjectAllocation.Evident
            // ReSharper disable HeapView.DelegateAllocation
            options.EnableVisualBasic(o => {
                // This setup will only run if the language is used, so branches
                // where no one ever used VB will be faster to open.
                var maxLanguageVersion = Enum.GetValues(typeof(LanguageVersion)).Cast<LanguageVersion>().Max();
                var features = _featureDiscovery.SlowDiscoverAll().ToDictionary(f => f, f => (string) null);

                o.ParseOptions = new VisualBasicParseOptions(maxLanguageVersion).WithFeatures(features);
                o.MetadataReferences = _referenceCollector.SlowGetMetadataReferencesRecursive(
                    typeof(StandardModuleAttribute).Assembly,
                    NetFrameworkRuntime.AssemblyOfValueTuple,
                    typeof(JitGenericAttribute).Assembly
                ).ToImmutableList();
            });
            // ReSharper restore HeapView.DelegateAllocation
            // ReSharper restore HeapView.ObjectAllocation.Evident
        }
    }
}
