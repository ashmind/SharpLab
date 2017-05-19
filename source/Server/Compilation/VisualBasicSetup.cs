using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using MirrorSharp;
using SharpLab.Runtime;
using SharpLab.Server.Compilation.Internal;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Compilation {
    public class VisualBasicSetup : IMirrorSharpSetup {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof(LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();
        
        private readonly ImmutableList<MetadataReference> _references;
        private readonly IReadOnlyDictionary<string, string> _features;

        public VisualBasicSetup(IMetadataReferenceCollector referenceCollector, IFeatureDiscovery featureDiscovery) {
            _references = referenceCollector.SlowGetMetadataReferencesRecursive(
                typeof(StandardModuleAttribute).Assembly,
                typeof(ValueTuple<>).Assembly,
                typeof(JitGenericAttribute).Assembly
            ).ToImmutableList();
            _features = featureDiscovery.SlowDiscoverAll().ToDictionary(f => f, f => (string)null);
        }

        public void ApplyTo(MirrorSharpOptions options) {
            // ReSharper disable HeapView.ObjectAllocation.Evident

            options.VisualBasic.ParseOptions = new VisualBasicParseOptions(MaxLanguageVersion).WithFeatures(_features);
            options.VisualBasic.CompilationOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            options.VisualBasic.MetadataReferences = _references;

            // ReSharper restore HeapView.ObjectAllocation.Evident
        }
    }
}
