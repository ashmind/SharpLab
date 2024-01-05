using SharpLab.Server.Monitoring;
using System.Collections.Generic;
using System.Linq;

namespace SharpLab.Server.Common;

public class FeatureTracker : IFeatureTracker {
    private readonly IMetricMonitor _branchMonitor;
    private readonly IReadOnlyDictionary<string, IMetricMonitor> _languageMetricMonitors;
    private readonly IReadOnlyDictionary<string, IMetricMonitor> _targetMetricMonitors;
    private readonly IMetricMonitor _optimizeDebugMonitor;
    private readonly IMetricMonitor _optimizeReleaseMonitor;

    public FeatureTracker(MetricMonitorFactory createMonitor, string webAppName) {
        _branchMonitor = createMonitor("feature", $"Branch: {webAppName}");
        _languageMetricMonitors = LanguageNames.All.ToDictionary(
            name => name,
            name => createMonitor("feature", $"Language: {name}")
        );
        _targetMetricMonitors = TargetNames.All.ToDictionary(
            name => name,
            name => createMonitor("feature", $"Target: {name}")
        );

        _optimizeDebugMonitor = createMonitor("feature", "Optimize: Debug");
        _optimizeReleaseMonitor = createMonitor("feature", "Optimize: Release");
    }

    public void TrackBranch() {
        _branchMonitor.Track(1);
    }

    public void TrackLanguage(string languageName) {
        if (_languageMetricMonitors.TryGetValue(languageName, out var metricMonitor))
            metricMonitor.Track(1);
    }

    public void TrackTarget(string targetName) {
        if (_targetMetricMonitors.TryGetValue(targetName, out var metricMonitor))
            metricMonitor.Track(1);
    }

    public void TrackOptimize(string? optimize) {
        var monitor = optimize switch {
            Optimize.Debug => _optimizeDebugMonitor,
            Optimize.Release => _optimizeReleaseMonitor,
            _ => null
        };
        monitor?.Track(1);
    }
}
