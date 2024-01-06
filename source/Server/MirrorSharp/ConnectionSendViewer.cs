using System;
using System.Threading;
using System.Threading.Tasks;
using MirrorSharp.Advanced;
using MirrorSharp.Advanced.EarlyAccess;
using SharpLab.Server.Caching;
using SharpLab.Server.Execution;

namespace SharpLab.Server.MirrorSharp; 
public class ConnectionSendViewer : IConnectionSendViewer {
    private readonly IResultCacher _cacher;
    private readonly IExceptionLogger _exceptionLogger;
    private readonly ICachingTracker _tracker;

    public ConnectionSendViewer(IResultCacher cacher, IExceptionLogger exceptionLogger, ICachingTracker tracker) {
        _cacher = cacher;
        _exceptionLogger = exceptionLogger;
        _tracker = tracker;
    }

    public Task ViewDuringSendAsync(string messageTypeName, ReadOnlyMemory<byte> message, IWorkSession session, CancellationToken cancellationToken) {
        if (messageTypeName != "slowUpdate")
            return Task.CompletedTask;

        if (session.HasCachingSeenSlowUpdateBefore())
            return Task.CompletedTask;

        // if update should not be cached, we will still not want to cache or measure the next one
        session.SetCachingHasSeenSlowUpdate();

        if (session.IsCachingDisabled()) {
            _tracker.TrackNoCacheRequest();
            return Task.CompletedTask;
        }

        if (!ShouldCache(session.GetLastSlowUpdateResult()))
            return Task.CompletedTask;

        _tracker.TrackCacheableRequest();
        return SafeCacheAsync(message, session, cancellationToken);
    }

    private bool ShouldCache(object? result) {
        return result is not ContainerExecutionResult { OutputFailed: true };
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
