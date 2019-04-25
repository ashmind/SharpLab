using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpLab.Server.Decompilation.Internal {
    public class JitAsmResultScope : IDisposable {
        private readonly IDisposable _lifetime;

        public JitAsmResultScope(IReadOnlyList<MethodJitResult> results, IDisposable lifetime) {
            Results = results;
            _lifetime = lifetime;
        }

        public IReadOnlyList<MethodJitResult> Results { get; }

        public void Dispose() {
            _lifetime.Dispose();
        }
    }
}
