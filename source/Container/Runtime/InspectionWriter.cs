using System.IO;
using System.Text;
using System.Text.Json;
using SharpLab.Runtime.Internal;

namespace SharpLab.Container.Runtime {
    internal class InspectionWriter : IInspectionWriter {
        private static class Strings {
            public static readonly JsonEncodedText Type = JsonEncodedText.Encode("type");
            public static readonly JsonEncodedText InspectionSimple = JsonEncodedText.Encode("inspection:simple");
            public static readonly JsonEncodedText InspectionMemory = JsonEncodedText.Encode("inspection:memory");
            public static readonly JsonEncodedText Title = JsonEncodedText.Encode("title");
            public static readonly JsonEncodedText Value = JsonEncodedText.Encode("value");
            public static readonly JsonEncodedText Labels = JsonEncodedText.Encode("labels");
            public static readonly JsonEncodedText Data = JsonEncodedText.Encode("data");
            public static readonly JsonEncodedText Name = JsonEncodedText.Encode("name");
            public static readonly JsonEncodedText Offset = JsonEncodedText.Encode("offset");
            public static readonly JsonEncodedText Length = JsonEncodedText.Encode("length");
            public static readonly JsonEncodedText Nested = JsonEncodedText.Encode("nested");
        }

        private readonly Stream _stream;
        private readonly Utf8JsonWriter _writer;

        public InspectionWriter(Stream stream) {
            _stream = stream;
            _writer = new Utf8JsonWriter(stream);
        }

        public void Write(SimpleInspection inspection) {
            WriteStartLine();
            _writer.WriteStartObject();
            _writer.WriteString(Strings.Type, Strings.InspectionSimple);
            _writer.WriteString(Strings.Title, inspection.Title);
            if (inspection.HasValue) {
                if (inspection.Value is string s) {
                    _writer.WriteString(Strings.Value, s);
                }
                else {
                    _writer.WriteString(Strings.Value, ((StringBuilder)inspection.Value!).ToString());
                }
            }
            _writer.WriteEndObject();
            WriteEndLineAndFlush();
        }

        public void Write(MemoryInspection inspection) {
            WriteStartLine();
            _writer.WriteStartObject();
            _writer.WriteString(Strings.Type, Strings.InspectionMemory);
            _writer.WriteString(Strings.Title, inspection.Title);

            _writer.WriteStartArray(Strings.Labels);
            foreach (var label in inspection.Labels) {
                WriteLabel(label);
            }
            _writer.WriteEndArray();

            _writer.WriteStartArray(Strings.Data);
            foreach (var @byte in inspection.Data) {
                _writer.WriteNumberValue(@byte);
            }
            _writer.WriteEndArray();

            _writer.WriteEndObject();
            WriteEndLineAndFlush();
        }

        private void WriteStartLine() {
            _stream.WriteByte((byte)'#');
        }

        private void WriteEndLineAndFlush() {
            _writer.Flush();
            _stream.WriteByte((byte)'\n');
            _stream.Flush();
            _writer.Reset();
        }

        private void WriteLabel(MemoryInspectionLabel label) {
            _writer.WriteStartObject();
            _writer.WriteString(Strings.Name, label.Name);
            _writer.WriteNumber(Strings.Offset, label.Offset);
            _writer.WriteNumber(Strings.Length, label.Length);
            if (label.Nested.Count > 0) {
                _writer.WriteStartArray(Strings.Nested);
                foreach (var nested in label.Nested) {
                    WriteLabel(nested);
                }
                _writer.WriteEndArray();
            }
            _writer.WriteEndObject();
        }
    }
}
