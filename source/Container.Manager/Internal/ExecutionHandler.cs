using System;
using System.Threading;
using System.Threading.Tasks;
using SharpLab.Container.Protocol.Stdin;

namespace SharpLab.Container.Manager.Internal {
    public class ExecutionHandler {
        private readonly ContainerPool _containerPool;
        private readonly StdinWriter _stdinWriter;
        private readonly StdoutReader _stdoutReader;
        private readonly ContainerCleanupWorker _cleanupWorker;

        public ExecutionHandler(
            ContainerPool containerPool,
            StdinWriter stdinWriter,
            StdoutReader stdoutReader,
            ContainerCleanupWorker cleanupWorker
        ) {
            _containerPool = containerPool;
            _stdinWriter = stdinWriter;
            _stdoutReader = stdoutReader;
            _cleanupWorker = cleanupWorker;
        }

        public async Task<ReadOnlyMemory<char>> ExecuteAsync(string sessionId, byte[] assemblyBytes, CancellationToken cancellationToken) {
            // Note that _containers are never accessed through multiple threads for the same session id,
            // so atomicity is not required within same session id
            if (_containerPool.GetSessionContainer(sessionId) is not {} container)
                container = await _containerPool.AllocateSessionContainerAsync(sessionId, _cleanupWorker.QueueForCleanup, cancellationToken);

            try {
                var (output, outputFailed) = await ExecuteInContainerAsync(container, assemblyBytes, cancellationToken);
                if (outputFailed)
                    _containerPool.RemoveSessionContainer(sessionId);
                return output;
            }
            catch {
                _containerPool.RemoveSessionContainer(sessionId);
                throw;
            }
        }

        private async Task<(ReadOnlyMemory<char> output, bool outputFailed)> ExecuteInContainerAsync(ActiveContainer container, byte[] assemblyBytes, CancellationToken cancellationToken) {
            var outputEndMarker = "---END-OUTPUT-" + Guid.NewGuid().ToString();

            await _stdinWriter.WriteCommandAsync(container.Stream, new ExecuteCommand(assemblyBytes, outputEndMarker), cancellationToken);

            using var executeCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            executeCancellationSource.CancelAfter(5000);
            return await _stdoutReader.ReadOutputAsync(container.Stream, outputEndMarker, executeCancellationSource.Token);
        }
    }
}
