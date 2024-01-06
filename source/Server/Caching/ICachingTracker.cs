namespace SharpLab.Server.Caching;

public interface ICachingTracker {
    void TrackBlobUploadError();
    void TrackBlobUploadRequest();
    void TrackCacheableRequest();
    void TrackNoCacheRequest();
}