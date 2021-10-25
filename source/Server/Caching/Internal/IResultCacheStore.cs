using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLab.Server.Caching.Internal {
    public interface IResultCacheStore {
        Task StoreAsync(string key, Stream stream, CancellationToken cancellationToken);
    }
}