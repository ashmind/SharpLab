using MirrorSharp.Advanced;
using Mono.Cecil;

namespace SharpLab.Server.Execution.Internal {
    public interface IContainerAssemblyRewriter {
        void Rewrite(AssemblyDefinition assembly, IWorkSession session);
    }
}
