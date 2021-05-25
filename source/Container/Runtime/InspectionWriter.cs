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
            public static readonly JsonEncodedText InspectionMemoryGraph = JsonEncodedText.Encode("inspection:memory-graph");
            public static readonly JsonEncodedText Title = JsonEncodedText.Encode("title");
            public static readonly JsonEncodedText Value = JsonEncodedText.Encode("value");
            public static readonly JsonEncodedText Labels = JsonEncodedText.Encode("labels");
            public static readonly JsonEncodedText Data = JsonEncodedText.Encode("data");
            public static readonly JsonEncodedText Name = JsonEncodedText.Encode("name");
            public static readonly JsonEncodedText Offset = JsonEncodedText.Encode("offset");
            public static readonly JsonEncodedText Length = JsonEncodedText.Encode("length");
            public static readonly JsonEncodedText Nested = JsonEncodedText.Encode("nested");
            public static readonly JsonEncodedText Stack = JsonEncodedText.Encode("stack");
            public static readonly JsonEncodedText Heap = JsonEncodedText.Encode("heap");
            public static readonly JsonEncodedText References = JsonEncodedText.Encode("references");
            public static readonly JsonEncodedText Id = JsonEncodedText.Encode("id");
            public static readonly JsonEncodedText Size = JsonEncodedText.Encode("size");
            public static readonly JsonEncodedText NestedNodes = JsonEncodedText.Encode("nestedNodes");
            public static readonly JsonEncodedText NestedNodesLimit = JsonEncodedText.Encode("nestedNodesLimit");
            public static readonly JsonEncodedText From = JsonEncodedText.Encode("from");
            public static readonly JsonEncodedText To = JsonEncodedText.Encode("to");
        }

        private readonly Stream _stream;
        private readonly Utf8JsonWriter _writer;

        public InspectionWriter(Stream stream) {
            _stream = stream;
            _writer = new Utf8JsonWriter(stream);
        }

        public void WriteSimple(SimpleInspection simple) {
            WriteStartLine();
            _writer.WriteStartObject();
            _writer.WriteString(Strings.Type, Strings.InspectionSimple);
            _writer.WriteString(Strings.Title, simple.Title);
            if (simple.HasValue) {
                if (simple.Value is string s) {
                    _writer.WriteString(Strings.Value, s);
                }
                else {
                    _writer.WriteString(Strings.Value, ((StringBuilder)simple.Value!).ToString());
                }
            }
            _writer.WriteEndObject();
            WriteEndLineAndFlush();
        }

        public void WriteMemory(MemoryInspection memory) {
            WriteStartLine();
            _writer.WriteStartObject();
            _writer.WriteString(Strings.Type, Strings.InspectionMemory);
            _writer.WriteString(Strings.Title, memory.Title);

            _writer.WriteStartArray(Strings.Labels);
            foreach (var label in memory.Labels) {
                WriteMemoryLabel(label);
            }
            _writer.WriteEndArray();

            _writer.WriteStartArray(Strings.Data);
            foreach (var @byte in memory.Data) {
                _writer.WriteNumberValue(@byte);
            }
            _writer.WriteEndArray();

            _writer.WriteEndObject();
            WriteEndLineAndFlush();
        }

        private void WriteMemoryLabel(MemoryInspectionLabel label) {
            _writer.WriteStartObject();
            _writer.WriteString(Strings.Name, label.Name);
            _writer.WriteNumber(Strings.Offset, label.Offset);
            _writer.WriteNumber(Strings.Length, label.Length);
            if (label.Nested.Count > 0) {
                _writer.WriteStartArray(Strings.Nested);
                foreach (var nested in label.Nested) {
                    WriteMemoryLabel(nested);
                }
                _writer.WriteEndArray();
            }
            _writer.WriteEndObject();
        }

        public void WriteMemoryGraph(MemoryGraphInspection graph) {
            WriteStartLine();

            _writer.WriteStartObject();
            _writer.WriteString(Strings.Type, Strings.InspectionMemoryGraph);
            _writer.WriteStartArray(Strings.Stack);
            foreach (var node in graph.Stack) {
                WriteMemoryGraphNode(node);
            }
            _writer.WriteEndArray();
            _writer.WriteStartArray(Strings.Heap);
            foreach (var node in graph.Heap) {
                WriteMemoryGraphNode(node);
            }
            _writer.WriteEndArray();
            _writer.WriteStartArray(Strings.References);
            foreach (var reference in graph.References) {
                WriteMemoryGraphReference(reference);
            }
            _writer.WriteEndArray();
            _writer.WriteEndObject();

            WriteEndLineAndFlush();
        }

        private void WriteMemoryGraphNode(MemoryGraphNode node) {
            _writer.WriteStartObject();
            _writer.WriteNumber(Strings.Id, node.Id);
            if (node.StackOffset != null) {
                _writer.WriteNumber(Strings.Offset, node.StackOffset.Value);
                _writer.WriteNumber(Strings.Size, node.StackSize!.Value);
            }
            _writer.WriteString(Strings.Title, node.Title);
            if (node.Value is string s) {
                _writer.WriteString(Strings.Value, s);
            }
            else {
                _writer.WriteString(Strings.Value, ((StringBuilder)node.Value!).ToString());
            }
            if (node.NestedNodes.Count > 0) {
                _writer.WriteStartArray(Strings.NestedNodes);
                foreach (var nested in node.NestedNodes) {
                    WriteMemoryGraphNode(nested);
                }
                _writer.WriteEndArray();

                if (node.NestedNodesLimitReached)
                    _writer.WriteBoolean(Strings.NestedNodesLimit, true);
            }
            _writer.WriteEndObject();
        }

        private void WriteMemoryGraphReference(MemoryGraphReference reference) {
            _writer.WriteStartObject();
            _writer.WriteNumber(Strings.From, reference.From.Id);
            _writer.WriteNumber(Strings.To, reference.To.Id);
            _writer.WriteEndObject();
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
    }
}
