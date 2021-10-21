using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MirrorSharp.Advanced;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Monitoring {
    public class DefaultLoggerMonitor : IMonitor {
        private readonly ILogger<DefaultLoggerMonitor> _logger;

        public DefaultLoggerMonitor(ILogger<DefaultLoggerMonitor> logger) {
            _logger = logger;
        }

        public void Metric(MonitorMetric metric, double value) {
            _logger.LogInformation("Metric {Namespace} {Name}: {Value}.", metric.Namespace, metric.Name, value);
        }

        public void Exception(Exception exception, IWorkSession? session, IDictionary<string, string>? extras = null) {
            _logger.LogError(exception, "[{SessionId}] Exception: {Message}", session?.GetSessionId(), exception.Message);
        }
    }
}
