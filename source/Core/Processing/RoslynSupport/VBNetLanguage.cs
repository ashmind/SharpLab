using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    [ThreadSafe]
    public class VBNetLanguage : IRoslynLanguage {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof(LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();

        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly IReadOnlyCollection<MetadataReference> _references = new[] {
            MetadataReference.CreateFromFile(typeof(StandardModuleAttribute).Assembly.Location)
        };

        public LanguageIdentifier Identifier => LanguageIdentifier.VBNet;

        public SyntaxTree ParseText(string code, SourceCodeKind kind) {
            var options = new VisualBasicParseOptions(
                kind: kind,
                languageVersion: MaxLanguageVersion
            );
            return VisualBasicSyntaxTree.ParseText(code, options);
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
