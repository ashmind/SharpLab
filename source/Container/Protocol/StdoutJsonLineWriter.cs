using System.IO;
using System.Text.Json;

namespace SharpLab.Container.Protocol {
    internal class StdoutJsonLineWriter {
        private readonly Stream _stream;
        private readonly Utf8JsonWriter _jsonWriter;

        public StdoutJsonLineWriter(Stream stream, Utf8JsonWriter jsonWriter) {
            _stream = stream;
            _jsonWriter = jsonWriter;
        }

        public Utf8JsonWriter StartJsonObjectLine() {
            _stream.WriteByte((byte)'#');
            _jsonWriter.WriteStartObject();
            return _jsonWriter;
        }

        public void EndJsonObjectLine() {
            _jsonWriter.WriteEndObject();
            _jsonWriter.Flush();
            _stream.WriteByte((byte)'\n');
            _stream.Flush();
            _jsonWriter.Reset();
        }
    }
}
