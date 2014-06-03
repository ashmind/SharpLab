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
        private readonly IRoslynAbstraction _roslynAbstraction;
        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly MetadataReference _microsoftCSharpReference;

        public CSharpLanguage(IRoslynAbstraction roslynAbstraction) {
            _roslynAbstraction = roslynAbstraction;
            _microsoftCSharpReference = _roslynAbstraction.NewMetadataFileReference(typeof(Binder).Assembly.Location);
        }

        public LanguageIdentifier Identifier {
            get { return LanguageIdentifier.CSharp; }
        }

        public SyntaxTree ParseText(string code, SourceCodeKind kind) {
            return _roslynAbstraction.ParseText(
                typeof(CSharpSyntaxTree),
                code, new CSharpParseOptions(_roslynAbstraction.GetMaxValue<LanguageVersion>(), kind: kind)
            );
        }

        public Compilation CreateLibraryCompilation(string assemblyName, bool optimizationsEnabled) {
            var options = _roslynAbstraction.NewCompilationOptions<CSharpCompilationOptions>(OutputKind.DynamicallyLinkedLibrary)
                                            .WithAllowUnsafe(true)
                                            .WithOptimizations(optimizationsEnabled);

            return CSharpCompilation.Create(assemblyName)
                                    .WithOptions(options)
                                    .AddReferences(_microsoftCSharpReference);
        }
    }
}
