using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    [ThreadSafe]
    public class CSharpLanguage : IRoslynLanguage {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof (LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();
        private static readonly IReadOnlyCollection<string> PreprocessorSymbols = new[] { "__DEMO_EXPERIMENTAL__" };

        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly IReadOnlyCollection<MetadataReference> _references = new[] {
            MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location)
        };

        public LanguageIdentifier Identifier => LanguageIdentifier.CSharp;

        public SyntaxTree ParseText(string code, SourceCodeKind kind) {
            var options = new CSharpParseOptions(
                kind: kind,
                languageVersion: MaxLanguageVersion,
                preprocessorSymbols: PreprocessorSymbols
            );
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
