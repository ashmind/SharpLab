using System;
using System.Text.Json;
using System.Threading;
using SharpLab.Container.Protocol;
using SharpLab.Runtime.Internal;

namespace SharpLab.Container.Runtime {
    using static JsonStrings;

    internal partial class FlowWriter : IFlowWriter {
        private static class Limits {
            public const int MaxRecords = 50;
            public const int MaxValuesPerLine = 3;

            public static readonly ValuePresenterLimits ValueName = new(maxValueLength: 10);
            public static readonly ValuePresenterLimits ValueValue = new(
                maxDepth: 2, maxEnumerableItemCount: 3, maxValueLength: 10
            );
        }

        private readonly StdoutJsonLineWriter _stdoutWriter;
        private readonly ContainerUtf8ValuePresenter _valuePresenter;
        private readonly FlowRecord[] _records = new FlowRecord[50];
        private readonly int[] _valueCountsPerLine = new int[75];
        private int _currentRecordIndex = -1;

        public FlowWriter(StdoutJsonLineWriter stdoutWriter, ContainerUtf8ValuePresenter valuePresenter) {
            _stdoutWriter = stdoutWriter;
            _valuePresenter = valuePresenter;
        }

        // Must be thread safe
        public void WriteLineVisit(int lineNumber) {
            TryAddRecord(new (lineNumber));
        }

        // Must be thread safe
        public void WriteValue<T>(T value, string? name, int lineNumber) {
            if (_valueCountsPerLine[lineNumber] > Limits.MaxValuesPerLine)
                return;

            var valueCountPerLine = Interlocked.Increment(ref _valueCountsPerLine[lineNumber]);
            if (valueCountPerLine > Limits.MaxValuesPerLine)
                return;

            if (valueCountPerLine == Limits.MaxValuesPerLine) {
                TryAddRecord(new (lineNumber, name, VariantValue.From("…")));
                return;
            }

            TryAddRecord(new (lineNumber, name, VariantValue.From(value)));
        }

        // Must be thread safe
        public void WriteSpanValue<T>(ReadOnlySpan<T> value, string? name, int lineNumber) {
            // TODO (can't store this, have to actually write it)
        }

        // Must be thread safe
        public void WriteException(object exception) {
            TryAddRecord(new (exception));
        }

        private bool TryAddRecord(FlowRecord record) {
            var nextRecordIndex = Interlocked.Increment(ref _currentRecordIndex);
            if (nextRecordIndex >= Limits.MaxRecords)
                return false;

            _records[nextRecordIndex] = record;
            return true;
        }

        // Does NOT have to be thread-safe
        public void FlushAndReset() {
            var recordCount = _currentRecordIndex + 1;

            _currentRecordIndex = -1;
            Array.Clear(_valueCountsPerLine, 0, _valueCountsPerLine.Length);

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

        private void WriteRecordToWriter(Utf8JsonWriter writer, FlowRecord record) {
            if (record.Value is {} value) {
                WriteRecordWithValueToWriter(writer, record, value);
                return;
            }

            if (record.Exception is {} exception) {
                writer.WriteStartObject();
                writer.WriteNumber(Line, record.LineNumber);
                writer.WriteString(Exception, record.Exception.GetType().Name);
                writer.WriteEndObject();
                return;
            }

            writer.WriteNumberValue(record.LineNumber);
        }

        private void WriteRecordWithValueToWriter(Utf8JsonWriter writer, FlowRecord record, VariantValue value) {
            writer.WriteStartObject();
            writer.WriteNumber(Line, record.LineNumber);
            if (record.Name != null)
                writer.WriteString(Name, record.Name);
            writer.WritePropertyName(Value);
            switch (value.Kind) {
                case VariantKind.Int32:
                    writer.WriteNumberValue(value.AsInt32Unchecked());
                    break;

                default:
                    var valueUtf8String = (Span<byte>)stackalloc byte[Limits.ValueValue.MaxValueLength];
                    _valuePresenter.Present(valueUtf8String, value, Limits.ValueValue, out var valueByteCount);
                    writer.WriteStringValue(valueUtf8String.Slice(0, valueByteCount));
                    break;
            }
            writer.WriteEndObject();
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
            }

            public FlowRecord(object exception) : this() {
                Exception = exception;
            }

            public int LineNumber { get; }
            public string? Name { get; }
            public VariantValue? Value { get; }
            public object? Exception { get; }
        }
    }
}
