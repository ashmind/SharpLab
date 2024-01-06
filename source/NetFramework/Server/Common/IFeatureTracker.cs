namespace SharpLab.Server.Common;

public interface IFeatureTracker {
    void TrackBranch();
    void TrackLanguage(string languageName);
    void TrackTarget(string targetName);
    void TrackOptimize(string optimize);
}