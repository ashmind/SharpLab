using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;
using SharpLab.Server.Caching.Internal;

namespace SharpLab.Server.Caching {
    public class ResultCacher : IResultCacher {
        private static class JsonKeys {
            public static readonly JsonEncodedText Version = JsonEncodedText.Encode("version");
            public static readonly JsonEncodedText Date = JsonEncodedText.Encode("date");
            public static readonly JsonEncodedText Encrypted = JsonEncodedText.Encode("encrypted");
            public static readonly JsonEncodedText IV = JsonEncodedText.Encode("iv");
            public static readonly JsonEncodedText Tag = JsonEncodedText.Encode("tag");
            public static readonly JsonEncodedText Data = JsonEncodedText.Encode("data");
        }

        private readonly IResultCacheBuilder _cacheBuilder;
        private readonly IResultCacheStore _cacheStore;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public ResultCacher(
            IResultCacheBuilder cacheBuilder,
            IResultCacheStore cacheStore,
            RecyclableMemoryStreamManager memoryStreamManager
        ) {
            _cacheBuilder = cacheBuilder;
            _cacheStore = cacheStore;
            _memoryStreamManager = memoryStreamManager;
        }

        public async Task CacheAsync(ResultCacheKeyData key, ReadOnlyMemory<byte> resultBytes, CancellationToken cancellationToken) {
            using var details = _cacheBuilder.Build(key, resultBytes);
            using var stream = _memoryStreamManager.GetStream();

            using var writer = new Utf8JsonWriter(stream);
            writer.WriteStartObject();
            writer.WriteNumber(JsonKeys.Version, 1);
            writer.WriteString(JsonKeys.Date, DateTimeOffset.Now);
            writer.WriteStartObject(JsonKeys.Encrypted);
            writer.WriteBase64String(JsonKeys.IV, details.IV.AsSpan());
            writer.WriteBase64String(JsonKeys.Tag, details.Tag.AsSpan());
            writer.WriteBase64String(JsonKeys.Data, details.EncryptedData.AsSpan());
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.Flush();
            stream.Position = 0;

            await _cacheStore.StoreAsync(
                details.CacheKey,
                stream,
                cancellationToken
            ).ConfigureAwait(false);
        }
    }
}
