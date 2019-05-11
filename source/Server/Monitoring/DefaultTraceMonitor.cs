using System;
using System.Collections.Generic;
using System.Diagnostics;
using MirrorSharp.Advanced;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Monitoring {
    public class DefaultTraceMonitor : IMonitor {
        public void Event(string name, IWorkSession? session, IDictionary<string, string>? extras = null) {
            Trace.TraceInformation("[{0}] Event '{0}'.", session?.GetSessionId(), name);
        }

        public void Exception(Exception exception, IWorkSession? session, IDictionary<string, string>? extras = null) {
            Trace.TraceError("[{0}] Exception: {0}.", session?.GetSessionId(), exception);
        }
    }
}
