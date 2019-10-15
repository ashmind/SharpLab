using System;
using System.Collections.Generic;
using SharpLab.Runtime.Internal;

partial class Inspect {
    public static void Allocations<T>(Func<T> action) {
        Allocations((Action)(() => action()));
    }

    public static unsafe void Allocations(Action action) {
        if (!InspectionSettings.Current.ProfilerActive)
            throw new NotSupportedException("Inspect.Allocations is only supported in .NET Core Profiler mode.");

        try {
            ProfilerNativeMethods.StartMonitoringCurrentThreadAllocations();
        }
        finally {
            ProfilerNativeMethods.StopMonitoringCurrentThreadAllocations(out var _, out var _, out var _, out var _);
        }

        int allocationCount;
        void* allocations;
        int totalAllocationCount;
        int totalAllocationBytes;
        ProfilerNativeMethods.AllocationMonitoringResult result;
        try {
            ProfilerNativeMethods.StartMonitoringCurrentThreadAllocations();
            action();
        }
        finally {
            result = ProfilerNativeMethods.StopMonitoringCurrentThreadAllocations(
                out allocationCount,
                out allocations,
                out totalAllocationCount,
                out totalAllocationBytes
            );
        }

        if (result == ProfilerNativeMethods.AllocationMonitoringResult.GC)
            Output.WriteWarning("Garbage collection has happened while retrieving allocations. Please try again.");

        if (allocationCount == 0) {
            Output.Write(new SimpleInspection("Allocations", "None"));
            return;
        }

        var runtime = GetRuntime();
        var inspections = new List<IInspection>(allocationCount);
        foreach (var allocationPointer in new Span<IntPtr>(allocations, allocationCount)) {
            // Note that profiler returns allocations pointers pointing to the start of the object
            // and not after initial header, as CLR does.
            var objectPointer = allocationPointer + IntPtr.Size /* object header size */;
            var @object = runtime.Heap.GetObject(unchecked((ulong)objectPointer.ToInt64()));

            inspections.Add(AllocationInspector.Inspect(@object));
        }

        var title = "Allocations: " + totalAllocationCount + " (" + totalAllocationBytes + " bytes)";
        Output.Write(new InspectionGroup(title, inspections, limitReached: totalAllocationCount > allocationCount));
    }
}