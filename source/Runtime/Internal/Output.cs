using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpLab.Runtime.Internal {
    public static class Output {
        private const int MaxStreamDataCount = 50;

        private static readonly List<object> _stream = new List<object>();
        public static IReadOnlyList<object> Stream => _stream;

        public static TextWriter Writer { get; } = new OutputWriter();

        public static void Write(InspectionResult data) {
            WriteObject(data);
        }

        public static void Write(string value) {
            WriteObject(value);
        }

        public static void Write(char[] value) {
            WriteObject(value);
        }

        private static void WriteObject(object value) {
            if (_stream.Count == MaxStreamDataCount - 1) {
                _stream.Add(new InspectionResult("System", new StringBuilder("Output limit reached")));
                return;
            }

            if (_stream.Count > MaxStreamDataCount - 1)
                return;
            _stream.Add(value);
        }

        private class OutputWriter : TextWriter {
            public override Encoding Encoding => Encoding.UTF8;

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