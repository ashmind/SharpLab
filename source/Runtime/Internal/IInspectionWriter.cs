namespace SharpLab.Runtime.Internal {
    internal interface IInspectionWriter {
        void Write(SimpleInspection inspection);
        void Write(MemoryInspection inspection);
    }
}
