using Microsoft.Extensions.Logging;

namespace SharpLab.Server.Monitoring;

public class DefaultLoggerMetricMonitor : IZeroDimensionMetricMonitor, IOneDimensionMetricMonitor {
    private readonly ILogger<DefaultLoggerMonitor> _logger;
    private readonly string _namespace;
    private readonly string _name;

    public DefaultLoggerMetricMonitor(
        ILogger<DefaultLoggerMonitor> logger,
        string @namespace, string name
    ) {
        Argument.NotNull(nameof(logger), logger);
        Argument.NotNullOrEmpty(nameof(@namespace), @namespace);
        Argument.NotNullOrEmpty(nameof(name), name);

        _logger = logger;
        _namespace = @namespace;
        _name = name;
    }

    public void Track(double value) {
        _logger.LogInformation("Metric {Namespace} {Name}: {Value}.", _namespace, _name, value);
    }

    public void Track(string dimension, double value) {
        _logger.LogInformation("Metric {Namespace} {Name}: {Dimension} {Value}.", _namespace, _name, dimension, value);
    }
}
