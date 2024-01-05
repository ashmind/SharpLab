using System;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;
using SharpLab.Server.Monitoring;

namespace SharpLab.Server.MirrorSharp; 
public class MonitorExceptionLogger : IExceptionLogger {
    private readonly IExceptionLogFilter _filter;
    private readonly IExceptionMonitor _monitor;

    public MonitorExceptionLogger(IExceptionLogFilter filter, IExceptionMonitor monitor) {
        _filter = filter;
        _monitor = monitor;
    }

    public void LogException(Exception exception, IWorkSession session) {
        if (!_filter.ShouldLog(exception, session))
            return;
        _monitor.Exception(exception, session);
    }
}
