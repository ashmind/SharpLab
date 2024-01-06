namespace SharpLab.Server.Monitoring;

public interface IMetricMonitor {
    void Track(double value);
}
