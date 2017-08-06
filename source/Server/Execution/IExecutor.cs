using System.IO;
using JetBrains.Annotations;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Execution {
    public interface IExecutor {
        [NotNull] ExecutionResult Execute([NotNull] Stream assemblyStream, [CanBeNull] Stream symbolStream, [NotNull] IWorkSession session);
        void Serialize([NotNull] ExecutionResult result, [NotNull] IFastJsonWriter writer);
    }
}