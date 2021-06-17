using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Execution.Container {
    public static class ContainerExperimentMetrics {
        public static MonitorMetric LegacyRunCount { get; } = new("container-experiment", "Runs: Legacy");
        public static MonitorMetric ContainerRunCount { get; } = new("container-experiment", "Runs: Container");
        public static MonitorMetric ContainerFailureCount { get; } = new("container-experiment", "Runs: Failed");
    }
}
