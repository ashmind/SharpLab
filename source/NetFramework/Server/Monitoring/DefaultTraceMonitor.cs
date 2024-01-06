using System;
using System.Collections.Generic;
using System.Diagnostics;
using MirrorSharp.Advanced;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Monitoring;

public class DefaultTraceMonitor : IMonitor {
    private readonly Func<(string @namespace, string name), DefaultTraceMetricMonitor> _createMetricMonitor;

    public DefaultTraceMonitor(
        Func<(string @namespace, string name), DefaultTraceMetricMonitor> createMetricMonitor
    ) {
        _createMetricMonitor = createMetricMonitor;
    }

    public IMetricMonitor MetricSlow(string @namespace, string name) {
        return _createMetricMonitor((@namespace, name));
    }

    public void Event(string eventName, IWorkSession? session, IDictionary<string, string>? extras = null) {
        Trace.TraceInformation("[{0}] Event: {1}.", session?.GetSessionId(), eventName);
    }

    public void Exception(Exception exception, IWorkSession? session, IDictionary<string, string>? extras = null) {
        Trace.TraceError("[{0}] Exception: {1}.", session?.GetSessionId(), exception);
    }
}
