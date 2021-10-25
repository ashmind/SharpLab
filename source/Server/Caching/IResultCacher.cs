using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLab.Server.Caching {
    public interface IResultCacher {
        Task CacheAsync(ResultCacheKeyData key, ReadOnlyMemory<byte> resultBytes, CancellationToken cancellationToken);
    }
}