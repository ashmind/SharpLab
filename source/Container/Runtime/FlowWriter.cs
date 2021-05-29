using System;
using System.Buffers.Text;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using SharpLab.Runtime.Internal;

namespace SharpLab.Container.Runtime {
    internal class FlowWriter : IFlowWriter {
        private static class Strings {
            public static readonly byte[] FlowPrefix = Encoding.UTF8.GetBytes("#fl:");
            public static readonly byte[][] Integers = Enumerable.Range(0, 25)
                .Select(n => Encoding.UTF8.GetBytes(n.ToString()))
                .ToArray();
        }

        private static class ReportLimits {
            public const int MaxStepCount = 50;
            public const int MaxStepNotesPerLine = 3;
            public const int MaxValuesPerStep = 3;

            public static readonly ValuePresenterLimits ValueName = new(maxValueLength: 10);
            public static readonly ValuePresenterLimits ValueValue = new(
                maxDepth: 2, maxEnumerableItemCount: 3, maxValueLength: 10
            );
        }

        private readonly Stream _stream;
        private readonly IValuePresenter _valuePresenter;
        //private readonly FlowStep[] _steps = new FlowStep[50];
        //private int _currentStepCount = 0;

        private int _stepCount = 0;

        public FlowWriter(Stream stream, IValuePresenter valuePresenter) {
            _stream = stream;
            _valuePresenter = valuePresenter;
        }

        public void WriteLineVisit(int lineNumber) {
            _stepCount += 1;
            if (_stepCount >= ReportLimits.MaxStepCount)
                return;
            //if (_currentStepCount >= ReportLimits.MaxStepCount)
            //    return;

            //_steps[_currentStepCount] = new FlowStep(lineNumber);

            _stream.Write(Strings.FlowPrefix);
            WriteLineNumber(lineNumber);
            _stream.WriteByte((byte)'\n');
            //_currentStepCount += 1;
        }

        public void WriteValue<T>(T value, string? name, int lineNumber) {
            _stepCount += 1;
            if (_stepCount >= ReportLimits.MaxStepCount)
                return;

            _stream.Write(Strings.FlowPrefix);
            WriteLineNumber(lineNumber);
            _stream.WriteByte((byte)':');            
            if (name != null)
                WriteUtf8String(name);
            _stream.WriteByte((byte)':');
            WriteUtf8String(_valuePresenter.ToStringBuilder(value, ReportLimits.ValueValue).ToString());
            _stream.WriteByte((byte)'\n');
        }

        public void WriteSpanValue<T>(ReadOnlySpan<T> value, string? name, int lineNumber) {
            _stepCount += 1;
            if (_stepCount >= ReportLimits.MaxStepCount)
                return;

            _stream.Write(Strings.FlowPrefix);
            WriteLineNumber(lineNumber);
            _stream.WriteByte((byte)':');
            if (name != null)
                WriteUtf8String(name);
            _stream.WriteByte((byte)':');
            WriteUtf8String(_valuePresenter.ToStringBuilder(value, ReportLimits.ValueValue).ToString());
            _stream.WriteByte((byte)'\n');
        }

        public void WriteException(object exception) {
            _stepCount += 1;
            if (_stepCount >= ReportLimits.MaxStepCount)
                return;

            _stream.Write(Strings.FlowPrefix);
            _stream.WriteByte((byte)'e');
            _stream.WriteByte((byte)':');
            WriteUtf8String(exception.GetType().Name);
            _stream.WriteByte((byte)'\n');
        }

        [SkipLocalsInit]
        private void WriteUtf8String(string value) {
            var byteSize = Encoding.UTF8.GetByteCount(value);
            Span<byte> valueBytes = stackalloc byte[byteSize];
            Encoding.UTF8.GetBytes(value, valueBytes);
            _stream.Write(valueBytes);
        }

        [SkipLocalsInit]
        private void WriteLineNumber(int lineNumber) {
            if (lineNumber >= Strings.Integers.Length) {
                Span<byte> lineNumberBytes = stackalloc byte[10];
                if (!Utf8Formatter.TryFormat(lineNumber, lineNumberBytes, out var lineNumberBytesLength))
                    throw new Exception($"Failed to format line number {lineNumber}.");
                _stream.Write(lineNumberBytes.Slice(0, lineNumberBytesLength));
            }
            else {
                _stream.Write(Strings.Integers[lineNumber]);
            }
        }

        public void Reset() {
            _stepCount = 0;
        }

        /*private readonly struct FlowStep {
            public FlowStep(int lineNumber) {
                LineNumber = lineNumber;
                Notes = null;
                Exception = null;
                ValueCount = 0;
                LineSkipped = false;
            }

            public int LineNumber { get; }
            public object? Exception { get; }
            public StringBuilder? Notes { get; }
            public bool LineSkipped { get; }
            internal int ValueCount { get; }
        }*/
    }
}
