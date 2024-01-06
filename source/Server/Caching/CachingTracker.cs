using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Caching;

public class CachingTracker : ICachingTracker {
    private readonly IMetricMonitor _cacheableRequestCountMonitor;
    private readonly IMetricMonitor _noCacheRequestCountMonitor;
    private readonly IMetricMonitor _blobUploadRequestCountMonitor;
    private readonly IMetricMonitor _blobUploadErrorCountMonitor;

    public CachingTracker(IMonitor monitor) {
        _cacheableRequestCountMonitor = monitor.MetricSlow("caching", "Caching: Cacheable Requests");
        _noCacheRequestCountMonitor = monitor.MetricSlow("caching", "Caching: No-Cache Requests");
        _blobUploadRequestCountMonitor = monitor.MetricSlow("caching", "Caching: Blob Upload Requests");
        _blobUploadErrorCountMonitor = monitor.MetricSlow("caching", "Caching: Blob Upload Errors");
    }

    public void TrackCacheableRequest() => _cacheableRequestCountMonitor.Track(1);
    public void TrackNoCacheRequest() => _noCacheRequestCountMonitor.Track(1);
    public void TrackBlobUploadRequest() => _blobUploadRequestCountMonitor.Track(1);
    public void TrackBlobUploadError() => _blobUploadErrorCountMonitor.Track(1);
}
