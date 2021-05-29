using System;

namespace SharpLab.Runtime.Internal {
    public static class ContainerFlow {
        public static void ReportLineStart(int lineNumber) {
            RuntimeServices.FlowWriter.WriteLineVisit(lineNumber);
        }

        public static void ReportRefValue<T>(ref T value, string? name, int lineNumber) {
            ReportValue(value, name, lineNumber);
        }

        public static void ReportValue<T>(T value, string? name, int lineNumber) {
            RuntimeServices.FlowWriter.WriteValue(value, name, lineNumber);
        }

        public static void ReportRefSpanValue<T>(ref Span<T> value, string? name, int lineNumber) {
            ReportReadOnlySpanValue((ReadOnlySpan<T>)value, name, lineNumber);
        }

        public static void ReportSpanValue<T>(Span<T> value, string? name, int lineNumber) {
            ReportReadOnlySpanValue((ReadOnlySpan<T>)value, name, lineNumber);
        }

        public static void ReportRefReadOnlySpanValue<T>(ref ReadOnlySpan<T> value, string? name, int lineNumber) {
            ReportReadOnlySpanValue(value, name, lineNumber);
        }

        public static void ReportReadOnlySpanValue<T>(ReadOnlySpan<T> value, string? name, int lineNumber) {
            RuntimeServices.FlowWriter.WriteSpanValue(value, name, lineNumber);
        }

        public static void ReportException(object exception) {
            RuntimeServices.FlowWriter.WriteException(exception);
        }
    }
}
