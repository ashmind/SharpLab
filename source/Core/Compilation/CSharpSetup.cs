using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TryRoslyn.Core.Compilation.Internal;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace TryRoslyn.Core.Compilation {
    public class CSharpSetup : ILanguageSetup {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof (LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();
        private static readonly IReadOnlyCollection<string> PreprocessorSymbols = new[] { "__DEMO_EXPERIMENTAL__" };
        
        private readonly ImmutableList<MetadataReference> _references;

        private readonly IReadOnlyDictionary<string, string> _features;

        public CSharpSetup(IMetadataReferenceCollector referenceCollector, IFeatureDiscovery featureDiscovery) {
            _references = referenceCollector.SlowGetMetadataReferencesRecursive(
                typeof(Binder).Assembly,
                typeof(ValueTuple<>).Assembly
            ).ToImmutableList();
            _features = featureDiscovery.SlowDiscoverAll().ToDictionary(f => f, f => (string)null);
        }

        public string LanguageName => LanguageNames.CSharp;

        public ParseOptions GetParseOptions(SourceCodeKind kind) {
            return new CSharpParseOptions(
                kind: kind,
                languageVersion: MaxLanguageVersion,
                preprocessorSymbols: PreprocessorSymbols
            ).WithFeatures(_features);
        }

        public CompilationOptions GetCompilationOptions() {
            return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
        }

        public ImmutableList<MetadataReference> GetMetadataReferences() => _references;
    }
}
