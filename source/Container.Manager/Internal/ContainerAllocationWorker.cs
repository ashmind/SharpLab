using System;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SharpLab.Container.Manager.Internal {
    public class ContainerAllocationWorker : BackgroundService {
        private readonly ContainerPool _containerPool;
        private readonly DockerClientConfiguration _dockerClientConfiguration;
        private readonly ContainerNameFormat _containerNameFormat;
        private readonly ContainerCleanupWorker _containerCleanup;
        private readonly ILogger<ContainerAllocationWorker> _logger;

        public ContainerAllocationWorker(
            ContainerPool containerPool,
            DockerClientConfiguration dockerClientConfiguration,
            ContainerNameFormat containerNameFormat,
            ContainerCleanupWorker containerCleanup,
            ILogger<ContainerAllocationWorker> logger
        ) {
            _containerPool = containerPool;
            _dockerClientConfiguration = dockerClientConfiguration;
            _containerNameFormat = containerNameFormat;
            _containerCleanup = containerCleanup;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("ContainerAllocationWorker starting");
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    await _containerPool.PreallocatedContainersWriter.WaitToWriteAsync(stoppingToken);
                    var container = await CreateAndStartContainerAsync(stoppingToken);
                    await _containerPool.PreallocatedContainersWriter.WriteAsync(container, stoppingToken);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to pre-allocate next container, retryng in 1 minute.");
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
            _containerPool.PreallocatedContainersWriter.Complete();
        }

        private async Task<ActiveContainer> CreateAndStartContainerAsync(CancellationToken cancellationToken) {
            var containerName = _containerNameFormat.GenerateName();
            _logger.LogDebug($"Allocating container {containerName}");
            var client = _dockerClientConfiguration.CreateClient();
            string containerId;
            try {
                containerId = (await client.Containers.CreateContainerAsync(new CreateContainerParameters {
                    Name = containerName,
                    Image = "mcr.microsoft.com/dotnet/runtime:5.0",
                    Cmd = new[] { @"c:\\app\SharpLab.Container.exe" },

                    AttachStdout = true,
                    AttachStdin = true,
                    OpenStdin = true,
                    StdinOnce = true,

                    NetworkDisabled = true,
                    HostConfig = new HostConfig {
                        Mounts = new[] {
                            new Mount {
                                Source = AppDomain.CurrentDomain.BaseDirectory,
                                Target = @"c:\app",
                                Type = "bind",
                                ReadOnly = true
                            }
                        },
                        Memory = 50 * 1024 * 1024,
                        CPUQuota = 50000,

                        AutoRemove = true
                    }
                }, cancellationToken)).ID;
            }
            catch {
                client.Dispose();
                throw;
            }

            MultiplexedStream? stream = null;
            try {
                stream = await client.Containers.AttachContainerAsync(containerId, tty: false, new ContainerAttachParameters {
                    Stream = true,
                    Stdin = true,
                    Stdout = true
                }, cancellationToken);

                try {
                    await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), cancellationToken);
                }
                catch {
                    stream.Dispose();
                    throw;
                }
            }
            catch {
                _containerCleanup.QueueForCleanup(client, containerId, stream);
                throw;
            }

            return new(client, containerId, stream);
        }
    }
}
