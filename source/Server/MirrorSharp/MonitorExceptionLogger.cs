using System;
using System.Net.WebSockets;
using MirrorSharp.Advanced;
using SharpLab.Server.Monitoring;

namespace SharpLab.Server.MirrorSharp {
    public class MonitorExceptionLogger : IExceptionLogger {
        private readonly IMonitor _monitor;

        public MonitorExceptionLogger(IMonitor monitor) {
            _monitor = monitor;
        }

        public void LogException(Exception exception, IWorkSession session) {
            // Note/TODO: need to see if OperationCanceledException can be avoided
            // https://github.com/ashmind/SharpLab/issues/617
            if (exception is WebSocketException or OperationCanceledException)
                return;
            _monitor.Exception(exception, session);
        }
    }
}
