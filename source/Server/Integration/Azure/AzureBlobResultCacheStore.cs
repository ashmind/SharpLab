using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Azure.Storage.Blobs;
using SharpLab.Server.Caching.Internal;
using System;
using SharpLab.Server.Monitoring;
using SharpLab.Server.Caching;

namespace SharpLab.Server.Integration.Azure {
    public class AzureBlobResultCacheStore : IResultCacheStore, IDisposable {
        private readonly MemoryCache _alreadyCached = new(new MemoryCacheOptions());
        private readonly BlobContainerClient _containerClient;
        private readonly string _cachePathPrefix;
        private readonly IMonitor _monitor;

        public AzureBlobResultCacheStore(
            BlobContainerClient containerClient,
            string cachePathPrefix,
            IMonitor monitor
        ) {
            _containerClient = containerClient;
            _cachePathPrefix = cachePathPrefix;
            _monitor = monitor;
        }

        public Task StoreAsync(string key, Stream stream, CancellationToken cancellationToken) {
            Argument.NotNullOrEmpty(nameof(key), key);
            Argument.NotNull(nameof(stream), stream);

            // no need to retry if we already processed this one
            if (_alreadyCached.TryGetValue(key, out _))
                return Task.CompletedTask;

            // it's OK to do this before trying, if we failed before we don't want to retry either
            var currentCall = new object();
            if (_alreadyCached.GetOrCreate(key, e => Cache(e, currentCall)) != currentCall)
                return Task.CompletedTask;

            _monitor.Metric(CachingMetrics.BlobUploadRequestCount, 1);
            var path = $"{_cachePathPrefix}/{key}.json";
            try {
                return _containerClient.UploadBlobAsync(path, stream, cancellationToken);
            }
            catch {
                _monitor.Metric(CachingMetrics.BlobUploadErrorCount, 1);
                throw;
            }
        }

        private object Cache(ICacheEntry entry, object value) {
            entry.SlidingExpiration = TimeSpan.FromDays(1);
            return value;
        }

        public void Dispose() {
            _alreadyCached.Dispose();
        }
    }
}
