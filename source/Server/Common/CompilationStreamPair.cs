using System;
using System.IO;

namespace SharpLab.Server.Common {
    public class CompilationStreamPair : IDisposable {
        public CompilationStreamPair(Stream assemblyStream, Stream symbolStream) {
            AssemblyStream = Argument.NotNull(nameof(assemblyStream), assemblyStream);
            SymbolStream = symbolStream;
        }

        public Stream AssemblyStream { get; }
        public Stream SymbolStream { get; }

        public void Dispose() {
            AssemblyStream.Dispose();
            SymbolStream?.Dispose();
        }
    }
}
