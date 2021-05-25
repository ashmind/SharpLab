namespace SharpLab.Runtime.Internal {
    internal interface IInspectionWriter {
        void WriteSimple(SimpleInspection simple);
        void WriteMemory(MemoryInspection memory);
        void WriteMemoryGraph(MemoryGraphInspection graph);
    }
}
