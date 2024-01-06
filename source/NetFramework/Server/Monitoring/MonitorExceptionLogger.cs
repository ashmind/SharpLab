using System;
using System.Net.WebSockets;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Monitoring;

public class MonitorExceptionLogger : IExceptionLogger {
    private readonly IMonitor _monitor;

    public MonitorExceptionLogger(IMonitor monitor) {
        _monitor = monitor;
    }

    public void LogException(Exception exception, IWorkSession session) {
        if (exception is WebSocketException)
            return;
        _monitor.Exception(exception, session);
    }
}
