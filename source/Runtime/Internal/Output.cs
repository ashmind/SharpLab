using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SharpLab.Runtime.Internal {
    public static class Output {
        private const int MaxStreamDataCount = 50;

        private static readonly LazyAsyncLocal<IList<object>> _stream = new LazyAsyncLocal<IList<object>>(() => new List<object>());
        public static IReadOnlyList<object> Stream => (IReadOnlyList<object>?)_stream.ValueIfCreated ?? Array.Empty<object>();

        public static TextWriter Writer { get; } = new OutputWriter();

        public static void WriteWarning(string message) {
            Write(new SimpleInspection("Warning", message));
        }

        public static void Write(SimpleInspection inspection) {
            if (RuntimeServices.InspectionWriter is {} writer) {
                writer.Write(inspection);
                return;
            }

            WriteObject(inspection);
        }

        public static void Write(IInspection inspection) {
            WriteObject(inspection);
        }

        private static void Write(string value) {
            WriteObject(value);
        }

        private static void Write(char[] value) {
            WriteObject(value);
        }

        private static void WriteObject(object value) {
            var stream = _stream.Value;
            if (stream.Count == MaxStreamDataCount - 1) {
                stream.Add(new SimpleInspection("System", "Output limit reached"));
                return;
            }

            if (stream.Count > MaxStreamDataCount - 1)
                return;
            stream.Add(value);
        }

        public static void Reset() {
            _stream.ValueIfCreated?.Clear();
        }

        private class OutputWriter : TextWriter {
            public override Encoding Encoding => Encoding.UTF8;

            public OutputWriter() : base(CultureInfo.InvariantCulture) {
            }

            public override void Write(string value) {
                Output.Write(value);
            }

            public override void WriteLine(string value) {
                Output.Write(value);
                Output.Write(Environment.NewLine);
            }

            public override void Write(char value) {
                Output.Write(value.ToString());
            }

            public override void Write(char[] buffer) {
                Output.Write(buffer);
            }

            public override void Write(char[] buffer, int index, int count) {
                throw new NotSupportedException();
            }
        }
    }
}