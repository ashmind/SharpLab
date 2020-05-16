using Iced.Intel;
using Microsoft.Diagnostics.Runtime;

namespace SharpLab.Server.Decompilation.Internal {
    public class JitAsmSymbolResolver : Iced.Intel.ISymbolResolver {
        private readonly ClrRuntime _runtime;

        public JitAsmSymbolResolver(ClrRuntime runtime) {
            _runtime = runtime;
        }

        public bool TryGetSymbol(in Instruction instruction, int operand, int instructionOperand, ulong address, int addressSize, out SymbolResult symbol) {
            var method = _runtime.GetMethodByAddress(address);
            if (method == null) {
                symbol = default;
                return false;
            }

            symbol = new SymbolResult(address, method.GetFullSignature());
            return true;
        }
    }
}
