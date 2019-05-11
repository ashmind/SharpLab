using System;

namespace SharpLab.Server.Common {
    public static class ProfilerState {
        // hardcoded for now, must match CorProfiler::Initialize
        private const string ActiveFlags = "COR_PRF_ENABLE_OBJECT_ALLOCATED | COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST | COR_PRF_MONITOR_OBJECT_ALLOCATED | COR_PRF_MONITOR_GC";

        public static bool Active { get; } = Environment.GetEnvironmentVariable("CORECLR_ENABLE_PROFILING") != null;
        public static string? Flags { get; } = Active ? ActiveFlags : null;
    }
}
