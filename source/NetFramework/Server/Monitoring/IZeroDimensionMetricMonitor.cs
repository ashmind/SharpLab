namespace SharpLab.Server.Monitoring;

public interface IZeroDimensionMetricMonitor {
    void Track(double value);
}
