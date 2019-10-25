using System;
using System.ComponentModel;
using SharpLab.Runtime.Internal;

public static partial class Inspect {
    public static void Heap(object @object) {
        var inspection = RuntimeServices.MemoryBytesInspector.InspectHeap(@object);
        Output.Write(inspection);
    }

    public static unsafe void Stack<T>(in T value) {
        var inspection = RuntimeServices.MemoryBytesInspector.InspectStack(value);
        Output.Write(inspection);
    }

    public static void MemoryGraph<T>(in T value) {
        Output.Write(
            CreateMemoryGraphBuilder()
                .Add(value)
                .ToInspection()
        );
    }

    public static void MemoryGraph<T1, T2>(in T1 value1, in T2 value2) {
        Output.Write(
            CreateMemoryGraphBuilder()
                .Add(value1)
                .Add(value2)
                .ToInspection()
        );
    }

    public static void MemoryGraph<T1, T2, T3>(in T1 value1, in T2 value2, in T3 value3) {
        Output.Write(
            CreateMemoryGraphBuilder()
                .Add(value1)
                .Add(value2)
                .Add(value3)
                .ToInspection()
        );
    }

    public static void MemoryGraph<T1, T2, T3, T4>(in T1 value1, in T2 value2, in T3 value3, in T4 value4) {
        Output.Write(
            CreateMemoryGraphBuilder()
                .Add(value1)
                .Add(value2)
                .Add(value3)
                .Add(value4)
                .ToInspection()
        );
    }

    public static void MemoryGraph<T1, T2, T3, T4, T5>(in T1 value1, in T2 value2, in T3 value3, in T4 value4, in T5 value5) {
        Output.Write(
            CreateMemoryGraphBuilder()
                .Add(value1)
                .Add(value2)
                .Add(value3)
                .Add(value4)
                .Add(value5)
                .ToInspection()
        );
    }

    private static IMemoryGraphBuilder CreateMemoryGraphBuilder() {
        return RuntimeServices.MemoryGraphBuilderFactory(MemoryGraphArgumentNames.Collect());
    }

    public static void Allocations<T>(Func<T> action) {
        Allocations((Action)(() => action()));
    }

    public static unsafe void Allocations(Action action) {
        var inspection = RuntimeServices.AllocationInspector.InspectAllocations(action);
        Output.Write(inspection);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static new bool Equals(object a, object b) {
        throw new NotSupportedException();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static new bool ReferenceEquals(object objA, object objB) {
        throw new NotSupportedException();
    }
}