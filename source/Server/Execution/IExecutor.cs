using MirrorSharp.Advanced;
using SharpLab.Server.Common;

namespace SharpLab.Server.Execution {
    public interface IExecutor {
        ExecutionResult Execute(CompilationStreamPair streams, IWorkSession session);
        void Serialize(ExecutionResult result, IFastJsonWriter writer, IWorkSession session);
    }
}