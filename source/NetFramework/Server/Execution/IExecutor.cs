using JetBrains.Annotations;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;

namespace SharpLab.Server.Execution {
    public interface IExecutor {
        [NotNull] ExecutionResult Execute([NotNull] CompilationStreamPair streams, [NotNull] IWorkSession session);
        void Serialize([NotNull] ExecutionResult result, [NotNull] IFastJsonWriter writer);
    }
}