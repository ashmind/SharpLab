using System;
using System.Threading;
using System.Threading.Tasks;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;
using SharpLab.Server.Execution.Container;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Execution {
    public class ContainerExecutor : IContainerExecutor {
        private readonly ContainerClient _client;

        public ContainerExecutor(ContainerClient client) {
            _client = client;
        }

        public async Task<string> ExecuteAsync(CompilationStreamPair streams, IWorkSession session, CancellationToken cancellationToken) {
            if (!session.GetContainerExperimentAccessAllowed())
                throw new UnauthorizedAccessException("Current session is not allowed access to container experiment.");

            using (streams)
                return await _client.ExecuteAsync(session.GetSessionId(), streams.AssemblyStream, cancellationToken);
        }
    }
}