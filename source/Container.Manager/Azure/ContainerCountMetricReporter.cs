using System;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SharpLab.Container.Manager.Azure {
    public class ContainerCountMetricReporter : BackgroundService {
        private static readonly MetricIdentifier ContainerCountMetric = new("Custom Metrics", "Container Count");

        private readonly DockerClientConfiguration _dockerClientConfiguration;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<ContainerCountMetricReporter> _logger;

        public ContainerCountMetricReporter(
            DockerClientConfiguration dockerClientConfiguration,
            TelemetryClient telemetryClient,
            ILogger<ContainerCountMetricReporter> logger
        ) {
            _dockerClientConfiguration = dockerClientConfiguration;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    using var client = _dockerClientConfiguration.CreateClient();
                    var containers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = true });

                    _telemetryClient.GetMetric(ContainerCountMetric).TrackValue(containers.Count);
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
