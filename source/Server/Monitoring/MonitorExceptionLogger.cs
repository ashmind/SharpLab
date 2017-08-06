using System;
using System.Collections.Generic;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Monitoring {
    public class MonitorExceptionLogger : IExceptionLogger {
        private readonly IMonitor _monitor;

        public MonitorExceptionLogger(IMonitor monitor) {
            _monitor = monitor;
        }

        public void LogException(Exception exception, IWorkSession session) {
            _monitor.Exception(exception, session);
        }
    }
}
