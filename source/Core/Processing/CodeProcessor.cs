using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using TryRoslyn.Core.Decompilation;
using TryRoslyn.Core.Processing.Languages;

namespace TryRoslyn.Core.Processing {
    [ThreadSafe]
    public class CodeProcessor : ICodeProcessor {
        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly MetadataReference[] _references = {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DynamicAttribute).Assembly.Location)
        };
        private readonly IReadOnlyCollection<IDecompiler> _decompilers;
        private readonly IReadOnlyCollection<IRoslynLanguage> _languages;
        
        public CodeProcessor(
            IReadOnlyCollection<IRoslynLanguage> languages,
            IReadOnlyCollection<IDecompiler> decompilers
        ) {
            _languages = languages;
            _decompilers = decompilers;
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

            var emitResult = compilation.Emit(stream);

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