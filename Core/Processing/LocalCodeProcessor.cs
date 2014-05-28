using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder;
using TryRoslyn.Core.Processing.RoslynSupport;

namespace TryRoslyn.Core.Processing {
    [ThreadSafe]
    public class LocalCodeProcessor : ICodeProcessor {
        private readonly IDecompiler _decompiler;
        private readonly IRoslynLanguage[] _languages;

        private readonly MetadataReference[] _references;

        public LocalCodeProcessor(IDecompiler decompiler, IRoslynAbstraction roslynAbstraction, params IRoslynLanguage[] languages) {
            _decompiler = decompiler;
            _languages = languages;

            _references = new MetadataReference[] {
                roslynAbstraction.NewMetadataFileReference(typeof(object).Assembly.Location),
                roslynAbstraction.NewMetadataFileReference(typeof(Uri).Assembly.Location),
                roslynAbstraction.NewMetadataFileReference(typeof(DynamicAttribute).Assembly.Location),
                roslynAbstraction.NewMetadataFileReference(typeof(Binder).Assembly.Location)
            };
        }

        public ProcessingResult Process(string code, ProcessingOptions options) {
            options = options ?? new ProcessingOptions();
            var kind = options.ScriptMode ? SourceCodeKind.Script : SourceCodeKind.Regular;
            var sourceLanguage = _languages.Single(l => l.Identifier == options.SourceLanguage);

            var syntaxTree = sourceLanguage.ParseText(code, kind);

            var stream = new MemoryStream();
            var emitResult = sourceLanguage
                .CreateUnsafeLibraryCompilation("Test", options.OptimizationsEnabled)
                .AddReferences(_references)
                .AddSyntaxTrees(syntaxTree)
                .Emit(stream);

            if (!emitResult.Success)
                return new ProcessingResult(null, emitResult.Diagnostics.Select(d => new SerializableDiagnostic(d)));

            stream.Seek(0, SeekOrigin.Begin);

            var resultWriter = new StringWriter();
            _decompiler.Decompile(stream, resultWriter, options.TargetLanguage);
            return new ProcessingResult(
                resultWriter.ToString(),
                emitResult.Diagnostics.Select(d => new SerializableDiagnostic(d))
            );
        }

        #region IDisposable Members

        void IDisposable.Dispose() {
        }

        #endregion
    }
}