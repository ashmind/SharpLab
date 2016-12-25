using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using TryRoslyn.Core.Processing.Languages.Internal;

namespace TryRoslyn.Core.Processing.Languages {
    [ThreadSafe]
    public class VBNetSetup : ILanguageSetup {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof(LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();

        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly IReadOnlyCollection<MetadataReference> _references;
        private readonly IReadOnlyDictionary<string, string> _features;

        public VBNetSetup(IMetadataReferenceCollector referenceCollector, IFeatureDiscovery featureDiscovery) {
            _references = referenceCollector.SlowGetMetadataReferencesRecursive(
                typeof(StandardModuleAttribute).Assembly,
                typeof(ValueTuple<>).Assembly
            ).ToArray();
            _features = featureDiscovery.SlowDiscoverAll().ToDictionary(f => f, f => (string)null);
        }

        public LanguageIdentifier Identifier => LanguageIdentifier.VBNet;
        public string LanguageName => LanguageNames.VisualBasic;

        public ParseOptions GetParseOptions(SourceCodeKind kind) {
            return new VisualBasicParseOptions(
                kind: kind,
                languageVersion: MaxLanguageVersion
            ).WithFeatures(_features);
        }

        public Compilation CreateLibraryCompilation(string assemblyName, bool optimizationsEnabled) {
            var options = new VisualBasicCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: optimizationsEnabled ? OptimizationLevel.Release : OptimizationLevel.Debug
            );

            return VisualBasicCompilation.Create(assemblyName, options: options, references: _references);
        }
    }
}
