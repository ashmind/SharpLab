namespace SharpLab.Server.Monitoring;

public interface IOneDimensionMetricMonitor {
    void Track(string dimension, double value);
}
