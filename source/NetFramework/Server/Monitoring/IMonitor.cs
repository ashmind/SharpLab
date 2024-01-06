using System;
using System.Collections.Generic;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Monitoring;
public interface IMonitor {
    IMetricMonitor MetricSlow(string @namespace, string name);
    void Event(string eventName, IWorkSession? session, IDictionary<string, string>? extras = null);
    void Exception(Exception exception, IWorkSession? session, IDictionary<string, string>? extras = null);
}
