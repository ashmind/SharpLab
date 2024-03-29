using System.Diagnostics;

namespace SharpLab.Server.Monitoring;

public class DefaultTraceMetricMonitor : IZeroDimensionMetricMonitor, IOneDimensionMetricMonitor {
    private readonly string _namespace;
    private readonly string _name;

    public DefaultTraceMetricMonitor(string @namespace, string name) {
        Argument.NotNullOrEmpty(nameof(@namespace), @namespace);
        Argument.NotNullOrEmpty(nameof(name), name);

        _namespace = @namespace;
        _name = name;
    }

    public void Track(double value) {
        Trace.TraceInformation("Metric {0} {1}: {2}.", _namespace, _name, value);
    }

    public void Track(string dimension, double value) {
        Trace.TraceInformation("Metric {0} {1}: {2} {3}.", _namespace, _name, dimension, value);
    }
}
