using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SharpLab.Container.Manager.Azure {
    public class ContainerCountMetricReporter : BackgroundService {
        private static readonly string ContainerProcessName = Path.GetFileNameWithoutExtension(Container.Program.ExeFileName);
        private static readonly MetricIdentifier ContainerCountMetric = new("Custom Metrics", "Container Count");

        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<ContainerCountMetricReporter> _logger;

        public ContainerCountMetricReporter(
            TelemetryClient telemetryClient,
            ILogger<ContainerCountMetricReporter> logger
        ) {
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    var count = 0;
                    foreach (var process in Process.GetProcessesByName(ContainerProcessName)) {
                        count += 1;
                        process.Dispose();
                    }

                    _telemetryClient.GetMetric(ContainerCountMetric).TrackValue(count);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to report container count");
                    await Task.Delay(TimeSpan.FromMinutes(4), stoppingToken);
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
