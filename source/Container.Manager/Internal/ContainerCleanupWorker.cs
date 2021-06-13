using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SharpLab.Container.Manager.Internal {
    public class ContainerCleanupWorker : BackgroundService {
        private readonly DockerClient _dockerClient;
        private readonly ContainerNameFormat _containerNameFormat;
        private readonly ILogger<ContainerCleanupWorker> _logger;

        private readonly Channel<(string containerId, MultiplexedStream? stream)> _cleanupQueueChannel = Channel.CreateUnbounded<(string, MultiplexedStream?)>();

        public ContainerCleanupWorker(
            DockerClient dockerClient,
            ContainerNameFormat containerNameFormat,
            ILogger<ContainerCleanupWorker> logger
        ) {
            _dockerClient = dockerClient;
            _containerNameFormat = containerNameFormat;
            _logger = logger;
        } 

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("ContainerCleanupWorker starting");
            try {
                await RemovePreviousContainersOnStartupAsync();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to remove one or more previous containers.");
            }

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    var (containerId, stream) = await _cleanupQueueChannel.Reader.ReadAsync();
                    await StopContainerAndDisposeClientAsync(containerId, stream);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to cleanup next container.");
                }
            }
        }

        private async Task RemovePreviousContainersOnStartupAsync() {
            var removeTasks = new List<Task>();
            foreach (var container in await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true })) {
                var removableName = container.Names.FirstOrDefault(_containerNameFormat.IsNameFromPreviousManager);
                if (removableName is null)
                    continue;

                _logger.LogInformation($"Removing container from previous run: {removableName}");
                removeTasks.Add(TryStopContainerAsync(container.ID));
            }

            _logger.LogInformation("Waiting for removal to complete");
            await Task.WhenAll(removeTasks);
            _logger.LogInformation("Removal completed");
        }

        public void QueueForCleanup(string containerId, MultiplexedStream? stream) {
            var queued = _cleanupQueueChannel.Writer.TryWrite((containerId, stream));
            if (!queued)
                throw new Exception("Failed to queue synchronously -- this should not happen with the unbounded channel.");
        }

        public void QueueForCleanup(ActiveContainer container) {
            QueueForCleanup(container.ContainerId, container.Stream);
        }

        public async Task StopContainerAndDisposeClientAsync(string containerId, MultiplexedStream? stream) {
            try {
                stream?.Dispose();
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Failed to dispose stream for container {containerId}");
            }

            await TryStopContainerAsync(containerId);
        }

        private async Task TryStopContainerAsync(string containerId) {
            try {
                await _dockerClient.Containers.StopContainerAsync(
                    containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 1 }
                );
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Failed to stop container {containerId}");
            }
        }
    }
}
