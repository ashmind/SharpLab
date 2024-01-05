using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MirrorSharp.Advanced;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Monitoring; 
public class DefaultLoggerExceptionMonitor : IExceptionMonitor {
    private readonly ILogger<DefaultLoggerExceptionMonitor> _logger;

    public DefaultLoggerExceptionMonitor(ILogger<DefaultLoggerExceptionMonitor> logger) {
        _logger = logger;
    }

    public void Exception(Exception exception, IWorkSession? session, IDictionary<string, string>? extras = null) {
        _logger.LogError(exception, "[{SessionId}] Exception: {Message}", session?.GetSessionId(), exception.Message);
    }
}
