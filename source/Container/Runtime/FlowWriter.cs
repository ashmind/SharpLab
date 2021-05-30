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
            public const int MaxNameLength = 10;

            public static readonly ValuePresenterLimits Value = new(
                maxDepth: 2, maxEnumerableItemCount: 3, maxValueLength: 10
            );
        }

        private readonly StdoutJsonLineWriter _stdoutWriter;
        private readonly ContainerUtf8ValuePresenter _valuePresenter;
        private readonly byte[] _truncatedNameBytes = new byte[(Limits.MaxNameLength - 1) + Utf8Ellipsis.Length];
        private readonly FlowRecord[] _records = new FlowRecord[50];
        private readonly int[] _valueCountsPerLine = new int[75];
        private int _currentRecordIndex = -1;

        public FlowWriter(
            StdoutJsonLineWriter stdoutWriter,
            ContainerUtf8ValuePresenter valuePresenter
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
            if (_valueCountsPerLine[lineNumber] > Limits.MaxValuesPerLine)
                return;

            var valueCountPerLine = Interlocked.Increment(ref _valueCountsPerLine[lineNumber]);
            if (valueCountPerLine > Limits.MaxValuesPerLine) {
                if (valueCountPerLine == Limits.MaxValuesPerLine + 1)
                    TryAddRecord(new(lineNumber, null, VariantValue.From("…")));
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

        // Does NOT have to be thread-safe
        private void WriteRecordToWriter(Utf8JsonWriter writer, FlowRecord record) {
            if (record.Value is {} value) {
                WriteRecordWithValueToWriter(writer, record, value);
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
                    var valueUtf8String = (Span<byte>)stackalloc byte[Limits.Value.MaxValueLength + Utf8Ellipsis.Length - 1];
                    _valuePresenter.Present(valueUtf8String, value, Limits.Value, out var valueByteCount);
                    writer.WriteStringValue(valueUtf8String.Slice(0, valueByteCount));
                    break;
            }
            if (record.Name != null)
                WriteNameToWriter(writer, record.Name);
            writer.WriteEndArray();
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
