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
        private readonly DockerClientConfiguration _clientConfiguration;
        private readonly ContainerNameFormat _containerNameFormat;
        private readonly ILogger<ContainerCleanupWorker> _logger;

        private readonly Channel<(DockerClient client, string containerId, MultiplexedStream? stream)> _cleanupQueueChannel = Channel.CreateUnbounded<(DockerClient, string, MultiplexedStream?)>();

        public ContainerCleanupWorker(
            DockerClientConfiguration clientConfiguration,
            ContainerNameFormat containerNameFormat,
            ILogger<ContainerCleanupWorker> logger
        ) {
            _clientConfiguration = clientConfiguration;
            _containerNameFormat = containerNameFormat;
            _logger = logger;
        } 

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                await RemovePreviousContainersOnStartupAsync();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to remove one or more previous containers.");
            }

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    var (client, containerId, stream) = await _cleanupQueueChannel.Reader.ReadAsync();
                    await StopContainerAndDisposeClientAsync(client, containerId, stream);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to cleanup next container.");
                }
            }
        }

        private async Task RemovePreviousContainersOnStartupAsync() {
            using var client = _clientConfiguration.CreateClient();

            var removeTasks = new List<Task>();
            foreach (var container in await client.Containers.ListContainersAsync(new ContainersListParameters { All = true })) {
                var removableName = container.Names.FirstOrDefault(_containerNameFormat.IsNameFromPreviousManager);
                if (removableName is null)
                    continue;

                _logger.LogInformation($"Removing container from previous run: {removableName}");
                removeTasks.Add(TryStopContainerAsync(client, container.ID));
            }
            await Task.WhenAll(removeTasks);
        }

        public void QueueForCleanup(DockerClient client, string containerId, MultiplexedStream? stream) {
            var queued = _cleanupQueueChannel.Writer.TryWrite((client, containerId, stream));
            if (!queued)
                throw new Exception("Failed to queue synchronously -- this should not happen with the unbounded channel.");
        }

        public void QueueForCleanup(ActiveContainer container) {
            QueueForCleanup(container.Client, container.ContainerId, container.Stream);
        }

        public async Task StopContainerAndDisposeClientAsync(DockerClient client, string containerId, MultiplexedStream? stream) {
            try {
                stream?.Dispose();
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Failed to dispose stream for container {containerId}");
            }

            await TryStopContainerAsync(client, containerId);

            try {
                client.Dispose();
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Failed to dispose client for container {containerId}");
            }
        }

        private async Task TryStopContainerAsync(DockerClient client, string containerId) {
            try {
                await client.Containers.StopContainerAsync(
                    containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 1 }
                );
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Failed to stop container {containerId}");
            }
        }
    }
}
