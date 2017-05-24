using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AshMind.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.IO;
using MirrorSharp.Advanced;
using SharpLab.Server.Compilation;
using SharpLab.Server.Decompilation;

namespace SharpLab.Server.MirrorSharp {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class SlowUpdate : ISlowUpdateExtension {
        private readonly ICompiler _compiler;
        private readonly IReadOnlyDictionary<string, IDecompiler> _decompilers;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public SlowUpdate(ICompiler compiler, IReadOnlyCollection<IDecompiler> decompilers, RecyclableMemoryStreamManager memoryStreamManager) {
            _compiler = compiler;
            _decompilers = decompilers.ToDictionary(d => d.LanguageName);
            _memoryStreamManager = memoryStreamManager;
        }

        public async Task<object> ProcessAsync(IWorkSession session, IList<Diagnostic> diagnostics, CancellationToken cancellationToken) {
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                return null;

            var targetLanguageName = session.GetTargetLanguageName();
            var decompiler = _decompilers.GetValueOrDefault(targetLanguageName);
            if (decompiler == null)
                throw new NotSupportedException($"Target '{targetLanguageName}' is not (yet?) supported by this branch.");

            using (var stream = _memoryStreamManager.GetStream()) {
                if (!await _compiler.TryCompileToStreamAsync(stream, session, diagnostics, cancellationToken).ConfigureAwait(false))
                    return null;

                stream.Seek(0, SeekOrigin.Begin);

                var resultWriter = new StringWriter();
                decompiler.Decompile(stream, resultWriter);
                return resultWriter.ToString();
            }
        }

        public void WriteResult(IFastJsonWriter writer, object result) {
            if (result != null)
                writer.WriteProperty("decompiled", (string)result);
        }
    }
}