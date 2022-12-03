using System;
using System.IO;
using Mono.Cecil;

namespace SharpLab.Server.Execution.Internal {
    public readonly struct AssemblyStreamRewriteResult : IDisposable {
        private readonly ModuleDefinition _module;

        public AssemblyStreamRewriteResult(Stream stream, TimeSpan? elapsedTime, ModuleDefinition module) {
            Stream = stream;
            ElapsedTime = elapsedTime;
            _module = module;
        }

        public Stream Stream { get; }
        public TimeSpan? ElapsedTime { get; }

        public void Dispose() {
            var moduleDisposeException = (Exception?)null;
            try {
                _module.Dispose();
            }
            catch (Exception ex) {
                moduleDisposeException = ex;
            }

            try {
                Stream.Dispose();
            }
            catch (Exception ex) when (moduleDisposeException != null) {
                throw new AggregateException(moduleDisposeException, ex);
            }
        }
    }
}
