using System;

partial class Inspect {
    public static void Allocations<T>(Func<T> action) {
        Allocations((Action)(() => action()));
    }

    public static unsafe void Allocations(Action action) {
        throw new NotSupportedException("Inspect.Allocations is only supported in .NET Core Profiler mode.");
    }
}