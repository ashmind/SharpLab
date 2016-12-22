using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TryRoslyn.Core.Processing.Languages.Internal;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace TryRoslyn.Core.Processing.Languages {
    [ThreadSafe]
    public class CSharpLanguage : IRoslynLanguage {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof (LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();
        private static readonly IReadOnlyCollection<string> PreprocessorSymbols = new[] { "__DEMO_EXPERIMENTAL__" };

        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly IReadOnlyCollection<MetadataReference> _references;

        private readonly IReadOnlyDictionary<string, string> _features;

        public CSharpLanguage(IMetadataReferenceCollector referenceCollector, IFeatureDiscovery featureDiscovery) {
            _references = referenceCollector.SlowGetMetadataReferencesRecursive(
                typeof(Binder).Assembly,
                typeof(ValueTuple<>).Assembly
            ).ToArray();
            _features = featureDiscovery.SlowDiscoverAll().ToDictionary(f => f, f => (string)null);
        }

        public LanguageIdentifier Identifier => LanguageIdentifier.CSharp;

        public SyntaxTree ParseText(string code, SourceCodeKind kind) {
            var options = new CSharpParseOptions(
                kind: kind,
                languageVersion: MaxLanguageVersion,
                preprocessorSymbols: PreprocessorSymbols
            ).WithFeatures(_features);
            return CSharpSyntaxTree.ParseText(code, options);
        }

        public Compilation CreateLibraryCompilation(string assemblyName, bool optimizationsEnabled) {
            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: optimizationsEnabled ? OptimizationLevel.Release : OptimizationLevel.Debug,
                allowUnsafe: true
            );

            return CSharpCompilation.Create(assemblyName, options: options, references: _references);
        }
    }
}
