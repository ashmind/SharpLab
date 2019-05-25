using System;

namespace SharpLab.Server.Common {
    public static class ProfilerState {
        public static bool Active { get; } = Environment.GetEnvironmentVariable("CORECLR_ENABLE_PROFILING") != null;
    }
}
