using System;
#if NETCORE
using System.Collections.Generic;
using SharpLab.Runtime.Internal;
#endif

partial class Inspect {
    public static void Allocations<T>(Func<T> action) {
        Allocations((Action)(() => action()));
    }

    public static unsafe void Allocations(Action action) {
        #if NETCORE
        if (Environment.GetEnvironmentVariable("CORECLR_ENABLE_PROFILING") == null)
        #endif
            throw new NotSupportedException("Inspect.Allocations is only supported in .NET Core Profiler mode.");

        #if NETCORE
        try {
            ProfilerNativeMethods.StartMonitoringCurrentThreadAllocations();
        }
        finally {
            ProfilerNativeMethods.StopMonitoringCurrentThreadAllocations(out var _, out var _, out var _);
        }

        int allocationCount;
        void* allocations;
        byte allocationLimitReached;
        ProfilerNativeMethods.AllocationMonitoringResult result;
        try {
            Flow.ReportingPaused = true;
            ProfilerNativeMethods.StartMonitoringCurrentThreadAllocations();
            action();
        }
        finally {
            result = ProfilerNativeMethods.StopMonitoringCurrentThreadAllocations(out allocationCount, out allocations, out allocationLimitReached);
            Flow.ReportingPaused = false;
        }

        if (result == ProfilerNativeMethods.AllocationMonitoringResult.GC)
            Output.WriteWarning("Garbage collection has happened while retrieving allocations. Please try again.");

        if (allocationCount == 0) {
            Output.Write(new SimpleInspection("Allocations", "None"));
            return;
        }

        EnsureRuntime();
        var inspections = new List<IInspection>(allocationCount);
        foreach (var allocationPointer in new Span<IntPtr>(allocations, allocationCount)) {
            // Note that profiler returns allocations pointers pointing to the start of the object
            // and not after initial header, as CLR does.
            var objectPointer = allocationPointer + IntPtr.Size /* object header size */;
            var @object = _runtime.Heap.GetObject(unchecked((ulong)objectPointer.ToInt64()));

            inspections.Add(AllocationInspector.Inspect(@object));
        }

        Output.Write(new InspectionGroup("Allocations", inspections, allocationLimitReached == 1));
        #endif
    }
}