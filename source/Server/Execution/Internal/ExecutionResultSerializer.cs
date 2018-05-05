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
            TextWriter openStringWriter = null;
            foreach (var item in output) {
                switch (item) {
                    case SimpleInspectionResult inspection:
                        if (openStringWriter != null) {
                            openStringWriter.Close();
                            openStringWriter = null;
                        }
                        SerializeSimpleInspectionResult(inspection, writer);
                        break;
                    case MemoryInspectionResult memory:
                        if (openStringWriter != null) {
                            openStringWriter.Close();
                            openStringWriter = null;
                        }
                        SerializeMemoryInspectionResult(memory, writer);
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
                        if (openStringWriter != null) {
                            openStringWriter.Close();
                            openStringWriter = null;
                        }
                        writer.WriteValue("Unsupported output object type: " + item.GetType().Name);
                        break;
                }
            }
            openStringWriter?.Close();
        }

        private void SerializeSimpleInspectionResult(SimpleInspectionResult inspection, IFastJsonWriter writer) {
            writer.WriteStartObject();
            writer.WriteProperty("type", "inspection:simple");
            writer.WriteProperty("title", inspection.Title);
            writer.WritePropertyName("value");
            if (inspection.Value is StringBuilder builder) {
                writer.WriteValue(builder);
            }
            else {
                writer.WriteValue((string)inspection.Value);
            }
            writer.WriteEndObject();
        }

        private void SerializeMemoryInspectionResult(MemoryInspectionResult memory, IFastJsonWriter writer) {
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
    }
}
