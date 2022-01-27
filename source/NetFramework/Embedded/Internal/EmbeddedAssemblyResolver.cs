using System;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.Metadata;

namespace SharpLab.Embedded.Internal {
    internal class EmbeddedAssemblyResolver : IAssemblyResolver {
        public PEFile? Resolve(IAssemblyReference reference) {
            throw new NotImplementedException();
        }

        public Task<PEFile?> ResolveAsync(IAssemblyReference reference) {
            throw new NotImplementedException();
        }

        public PEFile? ResolveModule(PEFile mainModule, string moduleName) {
            throw new NotImplementedException();
        }

        public Task<PEFile?> ResolveModuleAsync(PEFile mainModule, string moduleName) {
            throw new NotImplementedException();
        }
    }
}
