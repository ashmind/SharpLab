using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLab.Server.Execution.Container {
    public interface IContainerClient {
        Task<string> ExecuteAsync(string sessionId, Stream assemblyStream, CancellationToken cancellationToken);
    }
}