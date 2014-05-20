using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    [ThreadSafe]
    public class CSharpLanguage : IRoslynLanguage {
        private readonly IRoslynAbstraction _roslynAbstraction;

        public CSharpLanguage(IRoslynAbstraction roslynAbstraction) {
            _roslynAbstraction = roslynAbstraction;
        }

        public LanguageIdentifier Identifier {
            get { return LanguageIdentifier.CSharp; }
        }

        public SyntaxTree ParseText(string code, SourceCodeKind kind) {
            return CSharpSyntaxTree.ParseText(
                code, options: new CSharpParseOptions(_roslynAbstraction.GetMaxValue<LanguageVersion>(), kind: kind)
            );
        }

        public Compilation CreateUnsafeLibraryCompilation(string assemblyName, bool optimizationsEnabled) {
            return CSharpCompilation.Create(assemblyName).WithOptions(
                _roslynAbstraction
                    .NewCompilationOptions<CSharpCompilationOptions>(OutputKind.DynamicallyLinkedLibrary)
                    .WithAllowUnsafe(true)
                    .WithOptimizations(optimizationsEnabled)
            );
        }
    }
}
