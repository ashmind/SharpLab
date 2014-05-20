using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    [ThreadSafe]
    public class VBNetLanguage : IRoslynLanguage {
        private readonly IRoslynAbstraction _roslynAbstraction;

        public VBNetLanguage(IRoslynAbstraction roslynAbstraction) {
            _roslynAbstraction = roslynAbstraction;
        }

        public LanguageIdentifier Identifier {
            get { return LanguageIdentifier.VBNet; }
        }

        public SyntaxTree ParseText(string code, SourceCodeKind kind) {
            return VisualBasicSyntaxTree.ParseText(
                code, options: new VisualBasicParseOptions(_roslynAbstraction.GetMaxValue<LanguageVersion>(), kind: kind)
            );
        }

        public Compilation CreateUnsafeLibraryCompilation(string assemblyName, bool optimizationsEnabled) {
            return VisualBasicCompilation.Create(assemblyName).WithOptions(
                _roslynAbstraction.NewCompilationOptions<VisualBasicCompilationOptions>(OutputKind.DynamicallyLinkedLibrary)
                                  .WithOptimizations(optimizationsEnabled)
            );
        }
    }
}
