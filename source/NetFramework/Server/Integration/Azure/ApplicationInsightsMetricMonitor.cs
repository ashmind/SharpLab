using Microsoft.ApplicationInsights;
using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Integration.Azure;

public class ApplicationInsightsMetricMonitor : IMetricMonitor {
    private readonly Metric _metric;

    public ApplicationInsightsMetricMonitor(Metric metric) {
        Argument.NotNull(nameof(metric), metric);

        _metric = metric;
    }

    public void Track(double value) {
        _metric.TrackValue(value);
    }
}