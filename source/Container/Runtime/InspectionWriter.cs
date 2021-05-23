using System.IO;
using System.Text;
using System.Text.Json;
using SharpLab.Runtime.Internal;

namespace SharpLab.Container.Runtime {
    internal class InspectionWriter : IInspectionWriter {
        private static class Strings {
            public static readonly JsonEncodedText Type = JsonEncodedText.Encode("type");
            public static readonly JsonEncodedText InspectionSimple = JsonEncodedText.Encode("inspection:simple");
            public static readonly JsonEncodedText Title = JsonEncodedText.Encode("title");
            public static readonly JsonEncodedText Value = JsonEncodedText.Encode("value");
        }

        private readonly Stream _stream;
        private readonly Utf8JsonWriter _writer;

        public InspectionWriter(Stream stream) {
            _stream = stream;
            _writer = new Utf8JsonWriter(stream);
        }

        public void Write(SimpleInspection inspection) {
            _stream.WriteByte((byte)'#');
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
            _writer.Flush();
            _stream.WriteByte((byte)'\n');
            _stream.Flush();
            _writer.Reset();
        }
    }
}
