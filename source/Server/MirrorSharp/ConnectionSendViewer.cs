using System;
using System.Threading;
using System.Threading.Tasks;
using MirrorSharp.Advanced;
using MirrorSharp.Advanced.EarlyAccess;
using SharpLab.Server.Caching;

namespace SharpLab.Server.MirrorSharp {
    public class ConnectionSendViewer : IConnectionSendViewer {
        private readonly IResultCacher _cacher;
        private readonly IExceptionLogger _exceptionLogger;

        public ConnectionSendViewer(IResultCacher cacher, IExceptionLogger exceptionLogger) {
            _cacher = cacher;
            _exceptionLogger = exceptionLogger;
        }

        public Task ViewDuringSendAsync(string messageTypeName, ReadOnlyMemory<byte> message, IWorkSession session, CancellationToken cancellationToken) {
            if (messageTypeName != "slowUpdate")
                return Task.CompletedTask;

            if (session.WasFirstSlowUpdateCached())
                return Task.CompletedTask;

            session.SetFirstSlowUpdateCached(true);
            return SafeCacheAsync(message, session, cancellationToken);
        }

        private async Task SafeCacheAsync(ReadOnlyMemory<byte> message, IWorkSession session, CancellationToken cancellationToken) {
            try {
                var key = new ResultCacheKeyData(
                    session.LanguageName,
                    session.GetTargetName()!,
                    session.GetOptimize()!,
                    session.GetText()
                );
                await _cacher.CacheAsync(key, message, cancellationToken);
            }
            catch (Exception ex) {
                _exceptionLogger.LogException(ex, session);
            }
        }
    }
}
