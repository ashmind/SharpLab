namespace SharpLab.Server.Monitoring;

public delegate IMetricMonitor MetricMonitorFactory(string @namespace, string name);
