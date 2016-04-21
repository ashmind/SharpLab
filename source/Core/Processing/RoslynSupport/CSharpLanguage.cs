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

        private readonly IRoslynAbstraction _roslynAbstraction;
        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly MetadataReference _microsoftCSharpReference;

        public CSharpLanguage(IRoslynAbstraction roslynAbstraction) {
            _roslynAbstraction = roslynAbstraction;
            _microsoftCSharpReference = MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location);
        }

        public LanguageIdentifier Identifier => LanguageIdentifier.CSharp;

        public SyntaxTree ParseText(string code, SourceCodeKind kind) {
            var options = new CSharpParseOptions(
                kind: kind,
                languageVersion: MaxLanguageVersion,
                preprocessorSymbols: PreprocessorSymbols
            );
            return _roslynAbstraction.ParseText(typeof(CSharpSyntaxTree), code, options);
        }

        public Compilation CreateLibraryCompilation(string assemblyName, bool optimizationsEnabled) {
            var options = _roslynAbstraction.NewCompilationOptions<CSharpCompilationOptions>(OutputKind.DynamicallyLinkedLibrary)
                                            .WithAllowUnsafe(true);
            options = _roslynAbstraction.WithOptimizationLevel(
                options, optimizationsEnabled ? OptimizationLevelAbstraction.Release : OptimizationLevelAbstraction.Debug
            );
            
            return CSharpCompilation.Create(assemblyName)
                                    .WithOptions(options)
                                    .AddReferences(_microsoftCSharpReference);
        }
    }
}
