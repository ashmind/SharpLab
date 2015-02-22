using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using TryRoslyn.Core.Decompilation;
using TryRoslyn.Core.Processing.RoslynSupport;

namespace TryRoslyn.Core.Processing {
    [ThreadSafe]
    public class LocalCodeProcessor : ICodeProcessor {
        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly MetadataReference[] _references;
        private readonly IReadOnlyCollection<IDecompiler> _decompilers;
        private readonly IRoslynAbstraction _roslynAbstraction;
        private readonly IReadOnlyCollection<IRoslynLanguage> _languages;
        
        public LocalCodeProcessor(
            IRoslynAbstraction roslynAbstraction,
            IReadOnlyCollection<IRoslynLanguage> languages,
            IReadOnlyCollection<IDecompiler> decompilers
        ) {
            _roslynAbstraction = roslynAbstraction;
            _languages = languages;
            _decompilers = decompilers;

            _references = new[] {
                roslynAbstraction.MetadataReferenceFromPath(typeof(object).Assembly.Location),
                roslynAbstraction.MetadataReferenceFromPath(typeof(Uri).Assembly.Location),
                roslynAbstraction.MetadataReferenceFromPath(typeof(DynamicAttribute).Assembly.Location),
                roslynAbstraction.MetadataReferenceFromPath(typeof(FormattableStringFactory).Assembly.Location)
            };
        }

        public ProcessingResult Process(string code, ProcessingOptions options) {
            options = options ?? new ProcessingOptions();
            var kind = options.ScriptMode ? SourceCodeKind.Script : SourceCodeKind.Regular;
            var sourceLanguage = _languages.Single(l => l.Identifier == options.SourceLanguage);

            var syntaxTree = sourceLanguage.ParseText(code, kind);

            var stream = new MemoryStream();
            var compilation = sourceLanguage
                .CreateLibraryCompilation("Test", options.OptimizationsEnabled)
                .AddReferences(_references)
                .AddSyntaxTrees(syntaxTree);

            var emitResult = _roslynAbstraction.Emit(compilation, stream);

            if (!emitResult.Success)
                return new ProcessingResult(null, emitResult.Diagnostics.Select(d => new ProcessingResultDiagnostic(d)));

            stream.Seek(0, SeekOrigin.Begin);

            var resultWriter = new StringWriter();
            var decompiler = _decompilers.Single(d => d.Language == options.TargetLanguage);
            decompiler.Decompile(stream, resultWriter);
            return new ProcessingResult(
                resultWriter.ToString(),
                emitResult.Diagnostics.Select(d => new ProcessingResultDiagnostic(d))
            );
        }

        #region IDisposable Members

        void IDisposable.Dispose() {
        }

        #endregion
    }
}