using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using SharpLab.Server.Caching.Internal;

namespace SharpLab.Server.Integration.Azure {
    public class AzureBlobResultCacheStore : IResultCacheStore {
        private readonly BlobContainerClient _containerClient;
        private readonly string _cachePathPrefix;

        public AzureBlobResultCacheStore(BlobContainerClient containerClient, string cachePathPrefix) {
            _containerClient = containerClient;
            _cachePathPrefix = cachePathPrefix;
        }

        public Task StoreAsync(string key, Stream stream, CancellationToken cancellationToken) {
            Argument.NotNullOrEmpty(nameof(key), key);
            Argument.NotNull(nameof(stream), stream);

            var path = $"{_cachePathPrefix}/{key}.json";
            return _containerClient.UploadBlobAsync(path, stream, cancellationToken);
        }
    }
}
