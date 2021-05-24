using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace SharpLab.Container.Manager.Internal {
    public class SessionDebugLog {
        private static readonly MemoryCache _historyCache = new("_");

        public void LogMessage(string sessionId, string message) {
            if (_historyCache.Get(sessionId) is not IList<string> messages) {
                messages = new List<string>();
                _historyCache.Set(sessionId, messages, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromHours(1) });
            }
            messages.Add(DateTime.Now.ToString("[HH:mm.fff] ") + message + "\n");
        }

        public IReadOnlyList<string> GetAllLogMessages(string sessionId) {
            return _historyCache.Get(sessionId) is IReadOnlyList<string> messages
                 ? messages
                 : Array.Empty<string>();
        }
    }
}
