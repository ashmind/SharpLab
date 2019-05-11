using System.Collections.Generic;
using System.IO;
using System.Text;
using MirrorSharp.Advanced;
using SharpLab.Runtime.Internal;

namespace SharpLab.Server.Execution.Internal {
    public class ExecutionResultSerializer {
        public void Serialize(ExecutionResult result, IFastJsonWriter writer) {
            writer.WriteStartObject();
            writer.WritePropertyStartArray("output");
            SerializeOutput(result.Output, writer);
            writer.WriteEndArray();

            writer.WritePropertyStartArray("flow");
            foreach (var step in result.Flow) {
                SerializeFlowStep(step, writer);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private void SerializeFlowStep(Flow.Step step, IFastJsonWriter writer) {
            if (step.Notes == null && step.Exception == null) {
                writer.WriteValue(step.LineNumber);
                return;
            }

            writer.WriteStartObject();
            writer.WriteProperty("line", step.LineNumber);
            if (step.LineSkipped)
                writer.WriteProperty("skipped", true);
            if (step.Notes != null)
                writer.WriteProperty("notes", step.Notes);
            if (step.Exception != null)
                writer.WriteProperty("exception", step.Exception.GetType().Name);
            writer.WriteEndObject();
        }

        private void SerializeOutput(IReadOnlyList<object> output, IFastJsonWriter writer) {
            TextWriter? openStringWriter = null;
            void CloseStringWriter() {
                if (openStringWriter != null) {
                    openStringWriter.Close();
                    openStringWriter = null;
                }
            }

            foreach (var item in output) {
                switch (item) {
                    case IInspection inspection:
                        CloseStringWriter();
                        SerializeInspection(inspection, writer);
                        break;
                    case string @string:
                        if (openStringWriter == null)
                            openStringWriter = writer.OpenString();
                        openStringWriter.Write(@string);
                        break;
                    case char[] chars:
                        if (openStringWriter == null)
                            openStringWriter = writer.OpenString();
                        openStringWriter.Write(chars);
                        break;
                    case null:
                        break;
                    default:
                        CloseStringWriter();
                        writer.WriteValue("Unsupported output object type: " + item.GetType().Name);
                        break;
                }
            }
            openStringWriter?.Close();
        }

        private void SerializeInspection(IInspection inspection, IFastJsonWriter writer) {
            switch (inspection) {
                case SimpleInspection simple:
                    SerializeSimpleInspection(simple, writer);
                    break;
                case MemoryInspection memory:
                    SerializeMemoryInspection(memory, writer);
                    break;
                case MemoryGraphInspection graph:
                    SerializeMemoryGraphInspection(graph, writer);
                    break;
                case InspectionGroup group:
                    SerializeInspectionGroup(group, writer);
                    break;
                default:
                    writer.WriteValue("Unsupported inspection type: " + inspection.GetType().Name);
                    break;
            }
        }

        private void SerializeSimpleInspection(SimpleInspection inspection, IFastJsonWriter writer) {
            writer.WriteStartObject();
            writer.WriteProperty("type", "inspection:simple");
            writer.WriteProperty("title", inspection.Title);
            if (inspection.HasValue) {
                writer.WritePropertyName("value");
                if (inspection.Value is StringBuilder builder) {
                    writer.WriteValue(builder);
                }
                else {
                    writer.WriteValue((string)inspection.Value!);
                }
            }
            writer.WriteEndObject();
        }

        private void SerializeMemoryInspection(MemoryInspection memory, IFastJsonWriter writer) {
            writer.WriteStartObject();
            writer.WriteProperty("type", "inspection:memory");
            writer.WriteProperty("title", memory.Title);
            writer.WritePropertyStartArray("labels");
            foreach (var label in memory.Labels) {
                SerializeMemoryInspectionLabel(writer, label);
            }
            writer.WriteEndArray();
            writer.WritePropertyStartArray("data");
            foreach (var @byte in memory.Data) {
                writer.WriteValue(@byte);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private void SerializeMemoryInspectionLabel(IFastJsonWriter writer, MemoryInspectionLabel label) {
            writer.WriteStartObject();
            writer.WriteProperty("name", label.Name);
            writer.WriteProperty("offset", label.Offset);
            writer.WriteProperty("length", label.Length);
            if (label.Nested.Count > 0) {
                writer.WritePropertyStartArray("nested");
                foreach (var nested in label.Nested) {
                    SerializeMemoryInspectionLabel(writer, nested);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }

        private void SerializeMemoryGraphInspection(MemoryGraphInspection graph, IFastJsonWriter writer) {
            writer.WriteStartObject();
            writer.WriteProperty("type", "inspection:memory-graph");
            writer.WritePropertyStartArray("stack");
            foreach (var node in graph.Stack) {
                SerializeMemoryGraphNode(writer, node);
            }
            writer.WriteEndArray();
            writer.WritePropertyStartArray("heap");
            foreach (var node in graph.Heap) {
                SerializeMemoryGraphNode(writer, node);
            }
            writer.WriteEndArray();
            writer.WritePropertyStartArray("references");
            foreach (var reference in graph.References) {
                SerializeMemoryGraphReference(writer, reference);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private void SerializeMemoryGraphNode(IFastJsonWriter writer, MemoryGraphNode node) {
            writer.WriteStartObject();
            writer.WriteProperty("id", node.Id);
            if (node.StackOffset != null) {
                writer.WriteProperty("offset", node.StackOffset.Value);
                writer.WriteProperty("size", node.StackSize!.Value);
            }
            writer.WriteProperty("title", node.Title);
            writer.WritePropertyName("value");
            if (node.Value is StringBuilder builder) {
                writer.WriteValue(builder);
            }
            else {
                writer.WriteValue((string)node.Value);
            }
            if (node.NestedNodes.Count > 0) {
                writer.WritePropertyStartArray("nestedNodes");
                foreach (var nested in node.NestedNodes) {
                    SerializeMemoryGraphNode(writer, nested);
                }
                writer.WriteEndArray();

                if (node.NestedNodesLimitReached)
                    writer.WriteProperty("nestedNodesLimit", true);
            }

            writer.WriteEndObject();
        }

        private void SerializeMemoryGraphReference(IFastJsonWriter writer, MemoryGraphReference reference) {
            writer.WriteStartObject();
            writer.WriteProperty("from", reference.From.Id);
            writer.WriteProperty("to", reference.To.Id);
            writer.WriteEndObject();
        }

        private void SerializeInspectionGroup(InspectionGroup group, IFastJsonWriter writer) {
            writer.WriteStartObject();
            writer.WriteProperty("type", "inspection:group");
            writer.WriteProperty("title", group.Title);
            writer.WritePropertyStartArray("inspections");
            foreach (var inspection in group.Inspections) {
                SerializeInspection(inspection, writer);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
