using SharpLab.Server.Common;
using SharpLab.Server.Decompilation;

namespace SharpLab.Embedded.Internal.Targets {
    internal class DecompilerTarget : ITarget {
        private readonly IEmbeddedDecompiler _decompiler;

        public DecompilerTarget(IEmbeddedDecompiler decompiler) {
            _decompiler = decompiler;
        }

        public void Decompile(SharpLabRequest request) {
            Argument.NotNull(nameof(request), request);

            _decompiler.DecompileType(request.AssemblyStream, request.SymbolStream, request.ReflectionTypeName, request.Output);
        }
    }
}
