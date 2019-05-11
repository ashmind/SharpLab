using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpLab.Runtime.Internal {
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
            out byte allocationLimitReached
        );
    }
}
