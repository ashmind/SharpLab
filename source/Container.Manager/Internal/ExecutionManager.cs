using System;
using System.Threading;
using System.Threading.Tasks;
namespace SharpLab.Container.Manager.Internal {
    public class ExecutionManager {
        private readonly ContainerPool _containerPool;
        private readonly ExecutionProcessor _executionProcessor;
        private readonly ContainerCleanupWorker _cleanupWorker;

        public ExecutionManager(
            ContainerPool containerPool,
            ExecutionProcessor executionProcessor,
            ContainerCleanupWorker cleanupWorker
        ) {
            _containerPool = containerPool;
            _executionProcessor = executionProcessor;
            _cleanupWorker = cleanupWorker;
        }

        public async Task<ReadOnlyMemory<char>> ExecuteAsync(string sessionId, byte[] assemblyBytes, CancellationToken cancellationToken) {
            // Note that _containers are never accessed through multiple threads for the same session id,
            // so atomicity is not required within same session id
            using var allocationCancellation = CancellationFactory.ContainerAllocation(cancellationToken);
            if (_containerPool.GetSessionContainer(sessionId) is not {} container) {
                try {
                    container = await _containerPool.AllocateSessionContainerAsync(sessionId, _cleanupWorker.QueueForCleanup, allocationCancellation.Token);
                }
                catch (OperationCanceledException ex) {
                    throw new Exception("Failed to allocate container within 5 seconds.", ex);
                }
            }

            try {
                var (output, outputFailed) = await _executionProcessor.ExecuteInContainerAsync(container, assemblyBytes, cancellationToken);
                if (outputFailed)
                    _containerPool.RemoveSessionContainer(sessionId);
                return output;
            }
            catch {
                _containerPool.RemoveSessionContainer(sessionId);
                throw;
            }
        }
    }
}
