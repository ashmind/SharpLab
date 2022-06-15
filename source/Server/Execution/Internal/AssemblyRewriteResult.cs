using System;
using System.IO;
using Mono.Cecil;

namespace SharpLab.Server.Execution.Internal {
    public readonly struct AssemblyStreamRewriteResult : IDisposable {
        private readonly AssemblyDefinition _assembly;

        public AssemblyStreamRewriteResult(Stream stream, TimeSpan? elapsedTime, AssemblyDefinition assembly) {
            Stream = stream;
            ElapsedTime = elapsedTime;
            _assembly = assembly;
        }

        public Stream Stream { get; }
        public TimeSpan? ElapsedTime { get; }

        public void Dispose() {
            var assemblyDisposeException = (Exception?)null;
            try {
                _assembly.Dispose();
            }
            catch (Exception ex) {
                assemblyDisposeException = ex;
            }

            try {
                Stream.Dispose();
            }
            catch (Exception ex) when (assemblyDisposeException != null) {
                throw new AggregateException(assemblyDisposeException, ex);
            }
        }
    }
}
