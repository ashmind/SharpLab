using System;

namespace SharpLab.Server.Caching.Internal {
    public interface IResultCacheBuilder {
        ResultCacheDetails Build(in ResultCacheKeyData key, ReadOnlyMemory<byte> resultBytes);
    }
}