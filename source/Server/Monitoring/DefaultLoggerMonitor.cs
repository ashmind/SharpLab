using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MirrorSharp.Advanced;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Monitoring; 
public class DefaultLoggerMonitor : IMonitor {
    private readonly Func<(string @namespace, string name), DefaultLoggerMetricMonitor> _createMetricMonitor;
    private readonly ILogger<DefaultLoggerMonitor> _logger;

    public DefaultLoggerMonitor(
        Func<(string @namespace, string name), DefaultLoggerMetricMonitor> createMetricMonitor,
        ILogger<DefaultLoggerMonitor> logger
    ) {
        _createMetricMonitor = createMetricMonitor;
        _logger = logger;
    }

    public IMetricMonitor MetricSlow(string @namespace, string name) {
        return _createMetricMonitor((@namespace, name));
    }

    public void Exception(Exception exception, IWorkSession? session, IDictionary<string, string>? extras = null) {
        _logger.LogError(exception, "[{SessionId}] Exception: {Message}", session?.GetSessionId(), exception.Message);
    }
}
