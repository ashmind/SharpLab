using System;
using System.Runtime.InteropServices;

namespace SharpLab.Server.Execution.Runtime {
    [Obsolete("Only used as a reference for future allocation support in Container.", true)]
    internal static class ProfilerNativeMethods {
        public enum AllocationMonitoringResult : int {
            OK = 0,
            GC = 1
        }

        [DllImport("SharpLab.Native.Profiler")]
        public static extern AllocationMonitoringResult StartMonitoringCurrentThreadAllocations();

        [DllImport("SharpLab.Native.Profiler")]
        public static unsafe extern AllocationMonitoringResult StopMonitoringCurrentThreadAllocations(
            out int allocationCount,
            out void* allocations,
            out int totalAllocationCount,
            out int totalAllocationBytes
        );
    }
}
