namespace SharpLab.Runtime.Internal {
    public interface IMemoryGraphBuilder {
        IMemoryGraphBuilder Add<T>(in T value);
        MemoryGraphInspection ToInspection();
    }
}