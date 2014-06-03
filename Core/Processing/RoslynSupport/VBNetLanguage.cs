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
        private readonly IRoslynAbstraction _roslynAbstraction;
        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly MetadataFileReference _microsoftVisualBasicReference;

        public VBNetLanguage(IRoslynAbstraction roslynAbstraction) {
            _roslynAbstraction = roslynAbstraction;
            _microsoftVisualBasicReference = _roslynAbstraction.NewMetadataFileReference(typeof(StandardModuleAttribute).Assembly.Location);
        }

        public LanguageIdentifier Identifier {
            get { return LanguageIdentifier.VBNet; }
        }

        public SyntaxTree ParseText(string code, SourceCodeKind kind) {
            return _roslynAbstraction.ParseText(
                typeof(VisualBasicSyntaxTree),
                code, new VisualBasicParseOptions(_roslynAbstraction.GetMaxValue<LanguageVersion>(), kind: kind)
            );
        }

        public Compilation CreateLibraryCompilation(string assemblyName, bool optimizationsEnabled) {
            var options = _roslynAbstraction.NewCompilationOptions<VisualBasicCompilationOptions>(OutputKind.DynamicallyLinkedLibrary)
                                            .WithOptimizations(optimizationsEnabled);

            return VisualBasicCompilation.Create(assemblyName)
                                         .WithOptions(options)
                                         .AddReferences(_microsoftVisualBasicReference);
        }
    }
}
