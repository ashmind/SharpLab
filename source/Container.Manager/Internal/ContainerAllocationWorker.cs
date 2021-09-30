using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fragile;

namespace SharpLab.Container.Manager.Internal {
    public class ContainerAllocationWorker : BackgroundService {
        private readonly ContainerPool _containerPool;
        private readonly IProcessRunner _processRunner;
        private readonly ExecutionProcessor _warmupExecutionProcessor;
        private readonly ContainerCleanupWorker _containerCleanup;
        private readonly ILogger<ContainerAllocationWorker> _logger;

        private readonly byte[] _warmupAssemblyBytes;

        public ContainerAllocationWorker(
            ContainerPool containerPool,
            IProcessRunner processRunner,
            ExecutionProcessor warmupExecutionProcessor,
            ContainerCleanupWorker containerCleanup,
            ILogger<ContainerAllocationWorker> logger
        ) {
            _containerPool = containerPool;
            _processRunner = processRunner;
            _warmupExecutionProcessor = warmupExecutionProcessor;
            _containerCleanup = containerCleanup;
            _logger = logger;

            _warmupAssemblyBytes = File.ReadAllBytes(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SharpLab.Container.Warmup.dll")
            );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("ContainerAllocationWorker starting");
            _processRunner.InitialSetup();

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
                    try {
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                    catch (TaskCanceledException cancelEx) when (cancelEx.CancellationToken == stoppingToken) {
                    }
                }
            }
            _containerPool.PreallocatedContainersWriter.Complete();
        }

        private async Task<ActiveContainer> CreateAndStartContainerAsync(CancellationToken cancellationToken) {
            _logger.LogDebug($"Allocating container");

            var processContainer = _processRunner.StartProcess();
            try {
                var activeContainer = new ActiveContainer(processContainer);

                var outputBuffer = ArrayPool<byte>.Shared.Rent(2048);
                try {
                    var result = await _warmupExecutionProcessor.ExecuteInContainerAsync(
                        activeContainer, _warmupAssemblyBytes, outputBuffer,
                        includePerformance: false, isWarmup: true,
                        cancellationToken
                    );
                    if (!result.IsOutputReadSuccess)
                        throw new Exception($"Warmup output failed:\r\n" + Encoding.UTF8.GetString(result.Output.Span) + Encoding.UTF8.GetString(result.OutputReadFailureMessage.Span));
                }
                finally {
                    ArrayPool<byte>.Shared.Return(outputBuffer);
                }

                _logger.LogDebug("Allocated container");

                return activeContainer;
            }
            catch {
                _containerCleanup.QueueForCleanup(processContainer);
                throw;
            }
        }
    }
}
