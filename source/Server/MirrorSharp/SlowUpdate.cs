using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.IO;
using MirrorSharp.Advanced;
using TryRoslyn.Server.Decompilation;

namespace TryRoslyn.Server.MirrorSharp {
    public class SlowUpdate : ISlowUpdateExtension {
        private readonly IReadOnlyCollection<IDecompiler> _decompilers;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public SlowUpdate(IReadOnlyCollection<IDecompiler> decompilers, RecyclableMemoryStreamManager memoryStreamManager) {
            _decompilers = decompilers;
            _memoryStreamManager = memoryStreamManager;
        }

        public async Task<object> ProcessAsync(IWorkSession session, IList<Diagnostic> diagnostics, CancellationToken cancellationToken) {
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                return null;

            var targetLanguageName = session.GetTargetLanguageName();
            var decompiler = _decompilers.First(d => d.LanguageName == targetLanguageName);
            var compilation = await session.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            using (var stream = _memoryStreamManager.GetStream()) {
                var emitResult = compilation.Emit(stream);
                if (!emitResult.Success) {
                    foreach (var diagnostic in emitResult.Diagnostics) {
                        diagnostics.Add(diagnostic);
                    }
                    return null;
                }

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