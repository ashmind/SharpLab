using System;
using System.IO;
using System.Reflection;
using AppDomainToolkit;
using AshMind.Extensions;
using Microsoft.IO;
using MirrorSharp.Advanced;
using Mono.Cecil;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Execution.Internal;
using Unbreakable;

namespace SharpLab.Server.Execution {
    public class Executor {
        private readonly FlowReportingRewriter _rewriter;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public Executor(FlowReportingRewriter rewriter, RecyclableMemoryStreamManager memoryStreamManager) {
            _rewriter = rewriter;
            _memoryStreamManager = memoryStreamManager;
        }

        public ExecutionResult Execute(Stream assemblyStream, Stream symbolStream) {
            AssemblyDefinition assembly;
            using (assemblyStream)
            using (symbolStream) {
                assembly = AssemblyDefinition.ReadAssembly(assemblyStream, new ReaderParameters {
                    ReadSymbols = true,
                    SymbolStream = symbolStream
                });
            }
            _rewriter.Rewrite(assembly);
            var guardToken = AssemblyGuard.Rewrite(assembly, new AssemblyGuardSettings {
                ApiRules = ApiRules.SafeDefaults()
                    .Namespace("SharpLab.Runtime.Internal", ApiAccess.Allowed)
                    .Namespace("System.Collections", ApiAccess.Neutral, n => n.Type(nameof(System.Collections.IEnumerator), ApiAccess.Allowed))
                    .Namespace("System", ApiAccess.Neutral, n => n.Type(nameof(System.IDisposable), ApiAccess.Allowed))
            });

            using (var guardedStream = _memoryStreamManager.GetStream()) {
                assembly.Write(guardedStream);
                //assembly.Write(@"d:\Temp\assembly\" + DateTime.Now.Ticks + ".dll");
                guardedStream.Seek(0, SeekOrigin.Begin);

                var currentSetup = AppDomain.CurrentDomain.SetupInformation;
                using (var context = AppDomainContext.Create(new AppDomainSetup {
                    ApplicationBase = currentSetup.ApplicationBase,
                    PrivateBinPath = currentSetup.PrivateBinPath
                })) {
                    context.LoadAssembly(LoadMethod.LoadFrom, Assembly.GetExecutingAssembly().GetAssemblyFile().FullName);
                    return RemoteFunc.Invoke(context.Domain, guardedStream, guardToken, Remote.Execute);
                }
            }
        }

        public void Serialize(ExecutionResult result, IFastJsonWriter writer) {
            writer.WriteStartObject();
            if (result.Exception == null) {
                writer.WriteProperty("returnValue", result.ReturnValue);
            }
            else {
                writer.WriteProperty("exception", result.Exception.ToString());
            }
            writer.WritePropertyStartObject("lines");
            if (result.Lines != null) { // TODO (on exceptions)
                foreach (var line in result.Lines) {
                    writer.WritePropertyName(line.Key.ToString());
                    SerializeLine(line.Value, writer);
                }
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        private void SerializeLine(Flow.Line line, IFastJsonWriter writer) {
            if (!line.HasNotes && line.SingleVisit != null) {
                writer.WriteValue(line.SingleVisit.Value);
                return;
            }

            writer.WriteStartObject();
            if (line.HasNotes)
                writer.WriteProperty("notes", line.Notes);
            writer.WritePropertyStartArray("visits");
            if (line.SingleVisit != null) {
                writer.WriteValue(line.SingleVisit.Value);
            }
            else if (line.MultipleVisits != null /* TODO */) {
                foreach (var visit in line.MultipleVisits) {
                    writer.WriteValue(visit);
                }
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private static class Remote {
            public static ExecutionResult Execute(Stream assemblyStream, RuntimeGuardToken guardToken) {
                try {
                    var assembly = Assembly.Load(ReadAllBytes(assemblyStream));
                    var c = assembly.GetType("C");
                    var m = c.GetMethod("M");

                    using (guardToken.Scope()) {
                        var result = m.Invoke(Activator.CreateInstance(c), null);
                        return new ExecutionResult(result?.ToString(), Flow.Lines);
                    }
                }
                catch (Exception ex) {
                    return new ExecutionResult(ex, Flow.Lines);
                }
            }

            private static byte[] ReadAllBytes(Stream stream) {
                byte[] bytes;
                if (stream is MemoryStream memoryStream) {
                    bytes = memoryStream.GetBuffer();
                    if (bytes.Length != memoryStream.Length)
                        bytes = memoryStream.ToArray();
                    return bytes;
                }

                // we can't use ArrayPool here as this method is called in a temp AppDomain
                bytes = new byte[stream.Length];
                if (stream.Read(bytes, 0, (int)stream.Length) != bytes.Length)
                    throw new NotSupportedException();

                return bytes;
            }
        }
    }
}
