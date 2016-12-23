using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;
using MirrorSharp.Advanced;
using TryRoslyn.Core;
using TryRoslyn.Core.Decompilation;

namespace TryRoslyn.Web.Api.Integration {
    public class SlowUpdate : ICustomSlowUpdate {
        private readonly IReadOnlyCollection<IDecompiler> _decompilers;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public SlowUpdate(IReadOnlyCollection<IDecompiler> decompilers, RecyclableMemoryStreamManager memoryStreamManager) {
            _decompilers = decompilers;
            _memoryStreamManager = memoryStreamManager;
        }

        public async Task<object> PrepareAsync(IWorkSession session, CancellationToken cancellationToken) {
            var decompiler = _decompilers.First(d => d.Language == LanguageIdentifier.CSharp);
            var compilation = await session.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            using (var stream = _memoryStreamManager.GetStream()) {
                var emitResult = compilation.Emit(stream);
                if (!emitResult.Success)
                    throw new NotImplementedException();

                stream.Seek(0, SeekOrigin.Begin);

                var resultWriter = new StringWriter();
                decompiler.Decompile(stream, resultWriter);
                return resultWriter.ToString();
            }
        }

        public Task WriteAsync(IFastJsonWriter writer, object prepared, CancellationToken cancellationToken) {
            var decompiled = (string) prepared;
            writer.
        }
    }
}