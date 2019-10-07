using JetBrains.Annotations;
using MirrorSharp.Advanced;
using Mono.Cecil;

namespace SharpLab.Server.Execution.Internal {
    public interface IAssemblyRewriter {
        void Rewrite([NotNull] AssemblyDefinition assembly, [NotNull] IWorkSession session);
    }
}