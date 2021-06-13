using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace SharpLab.Container.Manager.Internal {
    public class CrashSuspensionManager {
        private const int InitialSuspensionSeconds = 15;
        private static readonly byte[][] SuspensionMessages = Enumerable.Range(0, InitialSuspensionSeconds + 1)
            .Select(s => Encoding.UTF8.GetBytes($"(Container crashed, next container will be available in {s} second{(s != 1 ? "s" : "")})"))
            .ToArray();

        private readonly ConcurrentDictionary<string, DateTime> _suspensions = new();

        public ExecutionOutputResult? GetSuspension(string sessionId, byte[] buffer) {
            if (!_suspensions.TryGetValue(sessionId, out var suspensionEndTime))
                return null;

            var secondsLeft = Math.Min((int)(suspensionEndTime - DateTime.Now).TotalSeconds, InitialSuspensionSeconds);
            if (secondsLeft <= 0) {
                _suspensions.TryRemove(sessionId, out _);
                return null;
            }

            return new(ReadOnlyMemory<byte>.Empty, SuspensionMessages[secondsLeft]);
        }

        public ExecutionOutputResult SetSuspension(string sessionId, ExecutionOutputResult result) {
            var endTime = DateTime.Now + TimeSpan.FromSeconds(InitialSuspensionSeconds);
            if (!_suspensions.TryAdd(sessionId, endTime))
                throw new Exception($"Concurrency conflict when trying to add suspension for session id {sessionId}");

            return new(result.Output, SuspensionMessages[InitialSuspensionSeconds]);
        }
    }
}
