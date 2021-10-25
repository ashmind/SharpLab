using System;
using SharpLab.Server.Common;

namespace SharpLab.Server.Caching {
    public readonly struct ResultCacheDetails : IDisposable {
        public ResultCacheDetails(string cacheKey, MemoryLease<byte> iv, MemoryLease<byte> tag, MemoryLease<byte> encryptedData) {
            CacheKey = cacheKey;
            IV = iv;
            Tag = tag;
            EncryptedData = encryptedData;
        }

        public string CacheKey { get; }
        public MemoryLease<byte> IV { get; }
        public MemoryLease<byte> Tag { get; }
        public MemoryLease<byte> EncryptedData { get; }

        public void Dispose() {
            IV.Dispose();
            Tag.Dispose();
            EncryptedData.Dispose();
        }
    }
}
