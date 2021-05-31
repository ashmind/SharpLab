using System;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace SharpLab.Container.Manager.Internal {
    public class ExecutionManager {
        private readonly ContainerPool _containerPool;
        private readonly DockerClient _dockerClient;
        private readonly ContainerNameFormat _containerNameFormat;
        private readonly ExecutionProcessor _executionProcessor;
        private readonly CrashSuspensionManager _crashSuspensionManager;
        private readonly ContainerCleanupWorker _cleanupWorker;

        public ExecutionManager(
            ContainerPool containerPool,
            DockerClient dockerClient,
            ContainerNameFormat containerNameFormat,
            ExecutionProcessor executionProcessor,
            CrashSuspensionManager crashSuspensionManager,
            ContainerCleanupWorker cleanupWorker
        ) {
            _containerPool = containerPool;
            _dockerClient = dockerClient;
            _containerNameFormat = containerNameFormat;
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
                if (_crashSuspensionManager.GetSuspension(sessionId, outputBufferBytes) is {} suspension)
                    return suspension;

                try {
                    container = await _containerPool.AllocateSessionContainerAsync(sessionId, _cleanupWorker.QueueForCleanup, allocationCancellation.Token);
                }
                catch (OperationCanceledException ex) {
                    throw new ContainerAllocationException("Failed to allocate container within 5 seconds.", _containerPool.LastContainerPreallocationException ?? ex);
                }

                await _dockerClient.Containers.RenameContainerAsync(container.ContainerId, new ContainerRenameParameters {
                    NewName = _containerNameFormat.GenerateSessionContainerName(sessionId)
                }, cancellationToken);
            }

            var result = await _executionProcessor.ExecuteInContainerAsync(
                container,
                assemblyBytes,
                outputBufferBytes,
                includePerformance,
                cancellationToken
            );

            if (!result.IsOutputReadSuccess) {
                var containerCrashed = false;
                try {
                    var response = await _dockerClient.Containers.InspectContainerAsync(container.ContainerId, cancellationToken);
                    containerCrashed = !response.State.Running;
                }
                catch (DockerContainerNotFoundException) {
                    containerCrashed = true;
                }
                if (containerCrashed)
                    _containerPool.RemoveSessionContainer(sessionId);

                return _crashSuspensionManager.SetSuspension(sessionId, result);
            }
            return result;
        }
    }
}
