using JetBrains.Annotations;
using Mono.Cecil;

namespace SharpLab.Server.Execution.Internal {
    public interface IFlowReportingRewriter {
        void Rewrite([NotNull] AssemblyDefinition assembly);
    }
}