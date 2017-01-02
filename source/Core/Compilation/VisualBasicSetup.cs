using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using TryRoslyn.Core.Compilation.Internal;

namespace TryRoslyn.Core.Compilation {
    public class VisualBasicSetup : ILanguageSetup {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof(LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();

        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly ImmutableList<MetadataReference> _references;
        private readonly IReadOnlyDictionary<string, string> _features;

        public VisualBasicSetup(IMetadataReferenceCollector referenceCollector, IFeatureDiscovery featureDiscovery) {
            _references = referenceCollector.SlowGetMetadataReferencesRecursive(
                typeof(StandardModuleAttribute).Assembly,
                typeof(ValueTuple<>).Assembly
            ).ToImmutableList();
            _features = featureDiscovery.SlowDiscoverAll().ToDictionary(f => f, f => (string)null);
        }

        public string LanguageName => LanguageNames.VisualBasic;

        public ParseOptions GetParseOptions() {
            return new VisualBasicParseOptions(MaxLanguageVersion).WithFeatures(_features);
        }

        public CompilationOptions GetCompilationOptions() {
            return new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        }

        public ImmutableList<MetadataReference> GetMetadataReferences() => _references;
    }
}
