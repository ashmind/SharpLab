using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Caching {
    public static class CachingMetrics {
        public static MonitorMetric CacheableRequestCount { get; } = new("caching", "Caching: Cacheable Requests");
        public static MonitorMetric NoCacheRequestCount { get; } = new("caching", "Caching: No-Cache Requests");
        public static MonitorMetric BlobUploadRequestCount { get; } = new("caching", "Caching: Blob Upload Requests");
        public static MonitorMetric BlobUploadErrorCount { get; } = new("caching", "Caching: Blob Upload Errors");
    }
}
