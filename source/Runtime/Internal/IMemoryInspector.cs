namespace SharpLab.Runtime.Internal {
    public interface IMemoryInspector {
        MemoryInspection InspectHeap(ulong address);
    }
}