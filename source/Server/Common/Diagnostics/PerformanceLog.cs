using System;
using System.Diagnostics;
using System.Threading;

namespace SharpLab.Server.Common.Diagnostics {
    public static class PerformanceLog {
        private static readonly AsyncLocal<(Stopwatch watch, Action<string, long> log)> _state = new AsyncLocal<(Stopwatch, Action<string, long>)>();

        [Conditional("DEBUG")]
        public static void Enable(Action<string, long> log) {
            _state.Value = (Stopwatch.StartNew(), log);
        }

        [Conditional("DEBUG")]
        public static void Checkpoint(string name) {
            var (watch, log) = _state.Value;
            if (log == null)
                return;

            log(name, watch.ElapsedTicks);
            watch.Restart();
        }
    }
}
