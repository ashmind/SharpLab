using System;
using System.Collections.Generic;
using System.Diagnostics;
using MirrorSharp.Advanced;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Monitoring {
    public class DefaultTraceMonitor : IMonitor {
        public void Metric(MonitorMetric metric, double value) {
            Trace.TraceInformation("Metric {0} {1}: {2}.", metric.Namespace, metric.Name, value);
        }

        public void Exception(Exception exception, IWorkSession? session, IDictionary<string, string>? extras = null) {
            Trace.TraceError("[{0}] Exception: {0}.", session?.GetSessionId(), exception);
        }
    }
}
