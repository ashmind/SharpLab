using System;
using System.ComponentModel;
using SharpLab.Runtime.Internal;

public static class SharpLabObjectExtensions {
    // LinqPad/etc compatibility only
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static T Dump<T>(this T value) {
        Output.Write(new SimpleInspection("Dump", ValuePresenter.ToStringBuilder(value)));
        return value;
    }

    public static void Inspect<T>(this T value, string? title = null) {
        Output.Write(new SimpleInspection(title ?? "Inspect", ValuePresenter.ToStringBuilder(value)));

        var lineNumber = Flow.GetLastReportedLineNumber();
        if (lineNumber != null)
            Flow.ReportValue(value, title, lineNumber.Value);
    }

    public static void Inspect<T>(this Span<T> value, string? title = null) {
        ((ReadOnlySpan<T>)value).Inspect(title);
    }

    public static void Inspect<T>(this ReadOnlySpan<T> value, string? title = null) {
        Output.Write(new SimpleInspection(title ?? "Inspect", ValuePresenter.ToStringBuilder(value)));

        var lineNumber = Flow.GetLastReportedLineNumber();
        if (lineNumber != null)
            Flow.ReportReadOnlySpanValue(value, title, lineNumber.Value);
    }
}
