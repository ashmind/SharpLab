using System;
using System.Buffers.Text;
using System.IO;
using System.Text.Json;

namespace SharpLab.Container.Protocol {
    internal class StdoutWriter {
        private readonly Stream _stream;
        private readonly Utf8JsonWriter _jsonWriter;

        public StdoutWriter(Stream stream, Utf8JsonWriter jsonWriter) {
            _stream = stream;
            _jsonWriter = jsonWriter;
        }

        public void WriteOutputStart(Guid marker) {
            _stream.WriteByte((byte)'['); // not used by parser, just for human-readability
            WriteMarker(marker);
        }

        public void WriteOutputEnd(Guid marker) {
            WriteMarker(marker);
            _stream.WriteByte((byte)']');
            _stream.Flush();
        }

        private void WriteMarker(Guid marker) {
            var markerBytes = (Span<byte>)stackalloc byte[36];
            Utf8Formatter.TryFormat(marker, markerBytes, out _);
            _stream.Write(markerBytes);
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
