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

        private readonly IRoslynAbstraction _roslynAbstraction;
        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly MetadataReference _microsoftVisualBasicReference;

        public VBNetLanguage(IRoslynAbstraction roslynAbstraction) {
            _roslynAbstraction = roslynAbstraction;
            _microsoftVisualBasicReference = _roslynAbstraction.MetadataReferenceFromPath(typeof(StandardModuleAttribute).Assembly.Location);
        }

        public LanguageIdentifier Identifier => LanguageIdentifier.VBNet;

        public SyntaxTree ParseText(string code, SourceCodeKind kind) {
            var options = new VisualBasicParseOptions(
                kind: kind,
                languageVersion: MaxLanguageVersion
            );
            return _roslynAbstraction.ParseText(typeof(VisualBasicSyntaxTree), code, options);
        }

        public Compilation CreateLibraryCompilation(string assemblyName, bool optimizationsEnabled) {
            var options = _roslynAbstraction.NewCompilationOptions<VisualBasicCompilationOptions>(OutputKind.DynamicallyLinkedLibrary);
            options = _roslynAbstraction.WithOptimizationLevel(
                options, optimizationsEnabled ? OptimizationLevelAbstraction.Release : OptimizationLevelAbstraction.Debug
            );

            return VisualBasicCompilation.Create(assemblyName)
                                         .WithOptions(options)
                                         .AddReferences(_microsoftVisualBasicReference);
        }
    }
}
