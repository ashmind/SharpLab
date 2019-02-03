using SharpLab.Runtime.Internal;

partial class Inspect {
    public static void MemoryGraph<T>(in T value) {
        Write(new MemoryGraphBuilder(MemoryGraphArgumentNames.Collect()).Add(value));
    }

    public static void MemoryGraph<T1, T2>(in T1 value1, in T2 value2) {
        Write(
            new MemoryGraphBuilder(MemoryGraphArgumentNames.Collect())
                .Add(value1)
                .Add(value2)
        );
    }

    public static void MemoryGraph<T1, T2, T3>(in T1 value1, in T2 value2, in T3 value3) {
        Write(
            new MemoryGraphBuilder(MemoryGraphArgumentNames.Collect())
                .Add(value1)
                .Add(value2)
                .Add(value3)
        );
    }

    private static void Write(MemoryGraphBuilder builder) {
        Output.Write(builder.ToInspection());
    }
}