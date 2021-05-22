using System;
using System.IO;
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
        private readonly string _containerExeShadowCopyPath;
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
            _containerExeShadowCopyPath = Path.Combine(Path.GetTempPath(), "SharpLab.Container", Guid.NewGuid().ToString("N"));
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            ShadowCopyContainerExe();
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

        private void ShadowCopyContainerExe() {
            Directory.CreateDirectory(_containerExeShadowCopyPath);
            foreach (var filePath in Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory)) {
                var copyFilePath = Path.Combine(_containerExeShadowCopyPath, Path.GetFileName(filePath));
                _logger.LogInformation("Shadow-copying {0} to {1}", filePath, copyFilePath);
                File.Copy(filePath, copyFilePath);
            }
        }

        private async Task<ActiveContainer> CreateAndStartContainerAsync(CancellationToken cancellationToken) {
            var memoryLimit = 50 * 1024 * 1024;
            var client = _dockerClientConfiguration.CreateClient();
            string containerId;
            try {
                containerId = (await client.Containers.CreateContainerAsync(new CreateContainerParameters {
                    Name = _containerNameFormat.GenerateName(),
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
                                Source = _containerExeShadowCopyPath,
                                Target = @"c:\app",
                                Type = "bind",
                                ReadOnly = true
                            }
                        },
                        Memory = memoryLimit,
                        //MemorySwap = memoryLimit,
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
