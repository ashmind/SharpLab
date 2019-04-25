using System;
using System.IO;

namespace SharpLab.Server.Common {
    public class CompilationStreamPair : IDisposable {
        public CompilationStreamPair(MemoryStream assemblyStream, MemoryStream symbolStream) {
            AssemblyStream = Argument.NotNull(nameof(assemblyStream), assemblyStream);
            SymbolStream = symbolStream;
        }

        public MemoryStream AssemblyStream { get; }
        public MemoryStream SymbolStream { get; }

        public void Dispose() {
            AssemblyStream.Dispose();
            SymbolStream?.Dispose();
        }
    }
}
