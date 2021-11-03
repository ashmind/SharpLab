using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLab.Server.Execution.Container {
    public interface IContainerClient {
        Task<ContainerExecutionResult> ExecuteAsync(string sessionId, Stream assemblyStream, bool includePerformance, CancellationToken cancellationToken);
    }
}