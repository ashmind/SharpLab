using System;
using System.Text.Json;
using System.Threading;
using SharpLab.Container.Protocol;
using SharpLab.Runtime.Internal;

namespace SharpLab.Container.Runtime {
    using static JsonStrings;

    internal partial class FlowWriter : IFlowWriter {
        private static class Limits {
            public const int MaxRecords = 200;
            public const int MaxNameLength = 20;

            public static readonly ValuePresenterLimits Value = new(maxValueLength: 10, maxEnumerableItemCount: 3);
        }

        private readonly StdoutWriter _stdoutWriter;
        private readonly Utf8ValuePresenter _valuePresenter;
        private readonly byte[] _truncatedNameBytes = new byte[(Limits.MaxNameLength - 1) + Utf8Ellipsis.Length];
        private readonly FlowRecord[] _records = new FlowRecord[Limits.MaxRecords];
        private int _currentRecordIndex = -1;

        public FlowWriter(
            StdoutWriter stdoutWriter,
            Utf8ValuePresenter valuePresenter
        ) {
            _stdoutWriter = stdoutWriter;
            _valuePresenter = valuePresenter;
        }

        // Must be thread safe
        public void WriteLineVisit(int lineNumber) {
            TryAddRecord(new (lineNumber));
        }

        // Must be thread safe
        public void WriteValue<T>(T value, string? name, int lineNumber) {
            TryAddRecord(new (lineNumber, name, VariantValue.From(value)));
        }

        // Must be thread safe
        public void WriteSpanValue<T>(ReadOnlySpan<T> value, string? name, int lineNumber) {
            // TODO (can't store this, have to actually write it)
        }

        // Must be thread safe
        public void WriteTag(FlowRecordTag tag) {
            TryAddRecord(new(tag));
        }

        // Must be thread safe
        public void WriteException(object exception) {
            TryAddRecord(new (exception));
        }

        // Must be thread safe
        private bool TryAddRecord(FlowRecord record) {
            if (_currentRecordIndex > Limits.MaxRecords)
                return false;

            var nextRecordIndex = Interlocked.Increment(ref _currentRecordIndex);
            if (nextRecordIndex >= Limits.MaxRecords)
                return false;

            _records[nextRecordIndex] = record;
            return true;
        }

        // Does NOT have to be thread-safe
        public void FlushAndReset() {
            var recordCount = Math.Min(_currentRecordIndex + 1, _records.Length);

            _currentRecordIndex = -1;

            if (recordCount == 0)
                return;

            var writer = _stdoutWriter.StartJsonObjectLine();
            writer.WriteStartArray(Flow);
            for (var i = 0; i < recordCount; i++) {
                WriteRecordToWriter(writer, _records[i]);
            }
            writer.WriteEndArray();
            _stdoutWriter.EndJsonObjectLine();
        }

        // Does NOT have to be thread-safe
        private void WriteRecordToWriter(Utf8JsonWriter writer, FlowRecord record) {
            if (record.Value is {} value) {
                WriteRecordWithValueToWriter(writer, record, value);
                return;
            }

            if (record.Tag is {} tag) {
                var code = tag switch {
                    //FlowJumpKind.JumpUp => JumpUpCode,
                    //FlowJumpKind.JumpDown => JumpDownCode,
                    FlowRecordTag.MethodStart => MethodStartTagCode,
                    FlowRecordTag.MethodReturn => MethodReturnTagCode,
                    _ => throw new NotSupportedException("Unknown flow jump type: " + tag.ToString())
                };
                writer.WriteStringValue(code);
                return;
            }

            if (record.Exception is {} exception) {
                writer.WriteStartObject();
                writer.WriteString(Exception, record.Exception.GetType().Name);
                writer.WriteEndObject();
                return;
            }

            writer.WriteNumberValue(record.LineNumber);
        }

        // Does NOT have to be thread-safe
        private void WriteRecordWithValueToWriter(Utf8JsonWriter writer, FlowRecord record, VariantValue value) {
            writer.WriteStartArray();
            writer.WriteNumberValue(record.LineNumber);
            switch (value.Kind) {
                case VariantKind.Int32:
                    writer.WriteNumberValue(value.AsInt32Unchecked());
                    break;

                default:
                    WriteOtherValueToWriter(writer, record, value);
                    break;
            }
            if (record.Name != null)
                WriteNameToWriter(writer, record.Name);
            writer.WriteEndArray();
        }

        private void WriteOtherValueToWriter(Utf8JsonWriter writer, FlowRecord record, VariantValue value) {
            var utf8Length = _valuePresenter.GetMaxOutputByteCount(Limits.Value);
            if (utf8Length > 100) { // just in case, don't want a StackOverflow if here is a bug somewhere
                writer.WriteStringValue("(#error#)");
                return;
            }

            var utf8Bytes = (Span<byte>)stackalloc byte[utf8Length];
            _valuePresenter.Present(utf8Bytes, value, Limits.Value, out var byteCount);
            writer.WriteStringValue(utf8Bytes.Slice(0, byteCount));
        }

        // Does NOT have to be thread-safe
        private void WriteNameToWriter(Utf8JsonWriter writer, string name) {
            if (name.Length > Limits.MaxNameLength) {
                CharBreakingUtf8Encoder.Encode(name, _truncatedNameBytes);
                Utf8Ellipsis.CopyTo(_truncatedNameBytes.AsSpan().Slice(Limits.MaxNameLength - 1));
                writer.WriteStringValue(_truncatedNameBytes);
                return;
            }

            writer.WriteStringValue(name);
        }

        private readonly struct FlowRecord {
            public FlowRecord(int lineNumber) : this() {
                LineNumber = lineNumber;
            }

            public FlowRecord(int lineNumber, string? name, VariantValue value) {
                LineNumber = lineNumber;
                Name = name;
                Value = value;
                Exception = default;
                Tag = default;
            }

            public FlowRecord(FlowRecordTag tag) : this() {
                Tag = tag;
            }

            public FlowRecord(object exception) : this() {
                Exception = exception;
            }

            public int LineNumber { get; }
            public string? Name { get; }
            public VariantValue? Value { get; }
            public FlowRecordTag? Tag { get; }
            public object? Exception { get; }

            // allocates -- debug only
            public override string ToString() {
                if (Exception != null)
                    return "{exception: " + Exception.GetType().Name + "}";

                if (Tag != null)
                    return "{tag: " + Tag + "}";

                if (Value != null)
                    return "{value}";

                return "{line: " + LineNumber + "}";
            }
        }
    }
}
