using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fragile;

namespace SharpLab.Container.Manager.Internal {
    public class ContainerCleanupWorker : BackgroundService {
        private readonly ILogger<ContainerCleanupWorker> _logger;

        private readonly Channel<IDisposable> _cleanupQueueChannel = Channel.CreateUnbounded<IDisposable>();

        public ContainerCleanupWorker(
            ILogger<ContainerCleanupWorker> logger
        ) {
            _logger = logger;
        } 

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("ContainerCleanupWorker starting");
            /*try {
                await RemovePreviousContainersOnStartupAsync();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to remove one or more previous containers.");
            }*/

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    var processContainer = await _cleanupQueueChannel.Reader.ReadAsync();
                    processContainer.Dispose();
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to cleanup next container.");
                }
            }
        }

        /*
        private async Task RemovePreviousContainersOnStartupAsync() {
            var removeTasks = new List<Task>();
            foreach (var process in await Process.) {
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
        */

        public void QueueForCleanup(IProcessContainer container) => QueueForCleanup((IDisposable)container);
        public void QueueForCleanup(ActiveContainer container) => QueueForCleanup((IDisposable)container);

        private void QueueForCleanup(IDisposable container) {
            var queued = _cleanupQueueChannel.Writer.TryWrite(container);
            if (!queued)
                throw new Exception("Failed to queue synchronously -- this should not happen with the unbounded channel.");
        }
    }
}
