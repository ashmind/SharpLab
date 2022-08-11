using System;

namespace SharpLab.Runtime.Internal {
    [Obsolete("Only preserved for binary compatbility with older branches.", error: true)]
    public static class ContainerFlow {
        public static void ReportLineStart(int lineNumber) => Flow.ReportLineStart(lineNumber);
        public static void ReportRefValue<T>(ref T value, string? name, int lineNumber) => Flow.ReportRefValue(ref value, name, lineNumber);
        public static void ReportValue<T>(T value, string? name, int lineNumber) => Flow.ReportValue(value, name, lineNumber);
        public static void ReportRefSpanValue<T>(ref Span<T> value, string? name, int lineNumber) => Flow.ReportRefSpanValue(ref value, name, lineNumber);
        public static void ReportSpanValue<T>(Span<T> value, string? name, int lineNumber) => Flow.ReportSpanValue(value, name, lineNumber);
        public static void ReportRefReadOnlySpanValue<T>(ref ReadOnlySpan<T> value, string? name, int lineNumber) => Flow.ReportRefReadOnlySpanValue(ref value, name, lineNumber);
        public static void ReportReadOnlySpanValue<T>(ReadOnlySpan<T> value, string? name, int lineNumber) => Flow.ReportReadOnlySpanValue(value, name, lineNumber);
        public static void ReportException(object exception) => Flow.ReportException(exception);
    }
}
