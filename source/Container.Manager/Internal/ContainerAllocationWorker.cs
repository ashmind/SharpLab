using System;
using System.Buffers;
using System.IO;
using System.Text;
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
        private readonly ExecutionProcessor _warmupExecutionProcessor;
        private readonly ContainerCleanupWorker _containerCleanup;
        private readonly ILogger<ContainerAllocationWorker> _logger;

        private readonly byte[] _warmupAssemblyBytes;

        public ContainerAllocationWorker(
            ContainerPool containerPool,
            DockerClientConfiguration dockerClientConfiguration,
            ContainerNameFormat containerNameFormat,
            ExecutionProcessor warmupExecutionProcessor,
            ContainerCleanupWorker containerCleanup,
            ILogger<ContainerAllocationWorker> logger
        ) {
            _containerPool = containerPool;
            _dockerClientConfiguration = dockerClientConfiguration;
            _containerNameFormat = containerNameFormat;
            _warmupExecutionProcessor = warmupExecutionProcessor;
            _containerCleanup = containerCleanup;
            _logger = logger;

            _warmupAssemblyBytes = File.ReadAllBytes(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SharpLab.Container.Warmup.dll")
            );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("ContainerAllocationWorker starting");
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    await _containerPool.PreallocatedContainersWriter.WaitToWriteAsync(stoppingToken);
                    var container = await CreateAndStartContainerAsync(stoppingToken);
                    await _containerPool.PreallocatedContainersWriter.WriteAsync(container, stoppingToken);
                    _containerPool.LastContainerPreallocationException = null;
                }
                catch (Exception ex) {
                    _containerPool.LastContainerPreallocationException = ex;
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
                        Isolation = "process",
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
            ActiveContainer container;
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

                container = new ActiveContainer(client, containerId, stream);

                var outputBuffer = ArrayPool<byte>.Shared.Rent(2048);
                try {
                    var result = await _warmupExecutionProcessor.ExecuteInContainerAsync(
                        container, _warmupAssemblyBytes, outputBuffer, includePerformance: false, cancellationToken
                    );
                    if (!result.IsOutputReadSuccess)
                        throw new Exception("Warmup output failed:\r\n" + Encoding.UTF8.GetString(result.Output.Span) + Encoding.UTF8.GetString(result.OutputReadFailureMessage.Span));
                }
                finally {
                    ArrayPool<byte>.Shared.Return(outputBuffer);
                }

                _logger.LogDebug($"Allocated container {containerName}");
            }
            catch {
                _containerCleanup.QueueForCleanup(client, containerId, stream);
                throw;
            }

            return container;
        }
    }
}
