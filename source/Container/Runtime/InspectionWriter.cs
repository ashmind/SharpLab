using System.Text;
using System.Text.Json;
using SharpLab.Container.Protocol;
using SharpLab.Runtime.Internal;

namespace SharpLab.Container.Runtime {
    using static JsonStrings;

    internal class InspectionWriter : IInspectionWriter {
        private readonly StdoutWriter _stdoutWriter;

        public InspectionWriter(StdoutWriter stdoutWriter) {
            _stdoutWriter = stdoutWriter;
        }

        public void WriteSimple(SimpleInspection simple) {
            var writer = _stdoutWriter.StartJsonObjectLine();
            writer.WriteString(Type, InspectionSimple);
            writer.WriteString(Title, simple.Title);
            if (simple.HasValue) {
                if (simple.Value is string s) {
                    writer.WriteString(Value, s);
                }
                else {
                    writer.WriteString(Value, ((StringBuilder)simple.Value!).ToString());
                }
            }
            _stdoutWriter.EndJsonObjectLine();
        }

        public void WriteMemory(MemoryInspection memory) {
            var writer = _stdoutWriter.StartJsonObjectLine();
            writer.WriteString(Type, InspectionMemory);
            writer.WriteString(Title, memory.Title);

            writer.WriteStartArray(Labels);
            foreach (var label in memory.Labels) {
                WriteMemoryLabel(writer, label);
            }
            writer.WriteEndArray();

            writer.WriteStartArray(Data);
            foreach (var @byte in memory.Data) {
                writer.WriteNumberValue(@byte);
            }
            writer.WriteEndArray();

            _stdoutWriter.EndJsonObjectLine();
        }

        private void WriteMemoryLabel(Utf8JsonWriter writer, MemoryInspectionLabel label) {
            writer.WriteStartObject();
            writer.WriteString(Name, label.Name);
            writer.WriteNumber(Offset, label.Offset);
            writer.WriteNumber(Length, label.Length);
            if (label.Nested.Count > 0) {
                writer.WriteStartArray(Nested);
                foreach (var nested in label.Nested) {
                    WriteMemoryLabel(writer, nested);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }

        public void WriteMemoryGraph(MemoryGraphInspection graph) {
            var writer = _stdoutWriter.StartJsonObjectLine();

            writer.WriteString(Type, InspectionMemoryGraph);
            writer.WriteStartArray(Stack);
            foreach (var node in graph.Stack) {
                WriteMemoryGraphNode(writer, node);
            }
            writer.WriteEndArray();
            writer.WriteStartArray(Heap);
            foreach (var node in graph.Heap) {
                WriteMemoryGraphNode(writer, node);
            }
            writer.WriteEndArray();
            writer.WriteStartArray(References);
            foreach (var reference in graph.References) {
                WriteMemoryGraphReference(writer, reference);
            }
            writer.WriteEndArray();

            _stdoutWriter.EndJsonObjectLine();
        }

        private void WriteMemoryGraphNode(Utf8JsonWriter writer, MemoryGraphNode node) {
            writer.WriteStartObject();
            writer.WriteNumber(Id, node.Id);
            if (node.StackOffset != null) {
                writer.WriteNumber(Offset, node.StackOffset.Value);
                writer.WriteNumber(Size, node.StackSize!.Value);
            }
            writer.WriteString(Title, node.Title);
            if (node.Value is string s) {
                writer.WriteString(Value, s);
            }
            else {
                writer.WriteString(Value, ((StringBuilder)node.Value!).ToString());
            }
            if (node.NestedNodes.Count > 0) {
                writer.WriteStartArray(NestedNodes);
                foreach (var nested in node.NestedNodes) {
                    WriteMemoryGraphNode(writer, nested);
                }
                writer.WriteEndArray();

                if (node.NestedNodesLimitReached)
                    writer.WriteBoolean(NestedNodesLimit, true);
            }
            writer.WriteEndObject();
        }

        private void WriteMemoryGraphReference(Utf8JsonWriter writer, MemoryGraphReference reference) {
            writer.WriteStartObject();
            writer.WriteNumber(From, reference.From.Id);
            writer.WriteNumber(To, reference.To.Id);
            writer.WriteEndObject();
        }
    }
}
