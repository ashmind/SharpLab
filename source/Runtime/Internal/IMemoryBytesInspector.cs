namespace SharpLab.Runtime.Internal {
    public interface IMemoryBytesInspector {
        MemoryInspection InspectHeap(object @object);
        MemoryInspection InspectStack<T>(in T value);
    }
}