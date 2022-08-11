using System;

namespace SharpLab.Runtime.Internal {
    internal interface IFlowWriter {
        void WriteArea(FlowAreaKind kind, int startLineNumber, int endLineNumber);
        void WriteLineVisit(int lineNumber);
        void WriteValue<T>(T value, string? name, int lineNumber);
        void WriteSpanValue<T>(ReadOnlySpan<T> value, string? name, int lineNumber);
        void WriteTag(FlowRecordTag tag);
        void WriteException(object exception);
    }
}
