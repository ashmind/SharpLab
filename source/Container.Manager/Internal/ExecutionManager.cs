using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLab.Container.Manager.Internal {
    public class ExecutionManager {
        private readonly ContainerPool _containerPool;
        private readonly ExecutionProcessor _executionProcessor;
        private readonly CrashSuspensionManager _crashSuspensionManager;
        private readonly ContainerCleanupWorker _cleanupWorker;

        public ExecutionManager(
            ContainerPool containerPool,
            ExecutionProcessor executionProcessor,
            CrashSuspensionManager crashSuspensionManager,
            ContainerCleanupWorker cleanupWorker
        ) {
            _containerPool = containerPool;
            _executionProcessor = executionProcessor;
            _crashSuspensionManager = crashSuspensionManager;
            _cleanupWorker = cleanupWorker;
        }

        public async Task<ExecutionOutputResult> ExecuteAsync(
            string sessionId,
            byte[] assemblyBytes,
            byte[] outputBufferBytes,
            bool includePerformance,
            CancellationToken cancellationToken
        ) {
            // Note that _containers are never accessed through multiple threads for the same session id,
            // so atomicity is not required within same session id
            using var allocationCancellation = CancellationFactory.ContainerAllocation(cancellationToken);
            if (_containerPool.GetSessionContainer(sessionId) is not {} container) {
                if (_crashSuspensionManager.GetSuspension(sessionId) is {} suspension)
                    return suspension;

                try {
                    container = await _containerPool.AllocateSessionContainerAsync(sessionId, _cleanupWorker.QueueForCleanup, allocationCancellation.Token);
                }
                catch (OperationCanceledException ex) {
                    throw new ContainerAllocationException("Failed to allocate container within 5 seconds.", _containerPool.LastContainerPreallocationException ?? ex);
                }
            }

            var result = await _executionProcessor.ExecuteInContainerAsync(
                container,
                assemblyBytes,
                outputBufferBytes,
                includePerformance,
                isWarmup: false,
                cancellationToken
            );

            if (!result.IsOutputReadSuccess) {
                if (container.Process.HasExited)
                    _containerPool.RemoveSessionContainer(sessionId);

                return _crashSuspensionManager.SetSuspension(sessionId, result);
            }
            return result;
        }
    }
}