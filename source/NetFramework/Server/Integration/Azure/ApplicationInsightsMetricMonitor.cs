using Microsoft.ApplicationInsights;
using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Integration.Azure;

public class ApplicationInsightsMetricMonitor : IZeroDimensionMetricMonitor, IOneDimensionMetricMonitor {
    private readonly Metric _metric;

    public ApplicationInsightsMetricMonitor(Metric metric) {
        Argument.NotNull(nameof(metric), metric);

        _metric = metric;
    }

    public void Track(double value) {
        _metric.TrackValue(value);
    }

    public void Track(string dimension, double value) {
        _metric.TrackValue(value, dimension);
    }
}