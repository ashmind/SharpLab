using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Common;

public class FeatureTracker : IFeatureTracker {
    private readonly string _webAppName;
    private readonly IOneDimensionMetricMonitor _branchMetricMonitor;
    private readonly IOneDimensionMetricMonitor _languageMetricMonitor;
    private readonly IOneDimensionMetricMonitor _targetMetricMonitor;
    private readonly IOneDimensionMetricMonitor _optimizeMetricMonitor;

    public FeatureTracker(IMonitor monitor, string webAppName) {
        _webAppName = webAppName;
        _branchMetricMonitor = monitor.MetricSlow("feature", "Branch", "Branch");
        _languageMetricMonitor = monitor.MetricSlow("feature", "Language", "Language");
        _targetMetricMonitor = monitor.MetricSlow("feature", "Target", "Target");
        _optimizeMetricMonitor = monitor.MetricSlow("feature", "Optimize", "Optimize");
    }

    public void TrackBranch() {
        _branchMetricMonitor.Track(_webAppName, 1);
    }

    public void TrackLanguage(string languageName) {
        _languageMetricMonitor.Track(languageName, 1);
    }

    public void TrackTarget(string targetName) {
        _targetMetricMonitor.Track(targetName, 1);
    }

    public void TrackOptimize(string optimize) {
        _optimizeMetricMonitor.Track(optimize, 1);
    }
}
