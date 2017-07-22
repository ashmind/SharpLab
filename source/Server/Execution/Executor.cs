using System;
using System.Collections.Generic;
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
            var guardToken = new RuntimeGuardToken(); /*AssemblyGuard.Rewrite(assembly, new AssemblyGuardSettings {
                ApiRules = ApiRules.SafeDefaults()
                    .Namespace("SharpLab.Runtime.Internal", ApiAccess.Allowed)
                    .Namespace("System.Collections", ApiAccess.Neutral, n => n.Type(nameof(System.Collections.IEnumerator), ApiAccess.Allowed))
                    .Namespace("System", ApiAccess.Neutral, n => n.Type(nameof(System.IDisposable), ApiAccess.Allowed))
            });*/

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
            if (result.Exception != null)
                writer.WriteProperty("exception", result.Exception.ToString());
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
                    case InspectionResult inspection:
                        if (openStringWriter != null) {
                            openStringWriter.Close();
                            openStringWriter = null;
                        }
                        writer.WriteStartObject();
                        writer.WriteProperty("type", "inspection");
                        writer.WriteProperty("title", inspection.Title);
                        writer.WriteProperty("value", inspection.Value);
                        writer.WriteEndObject();
                        break;
                    case string @string:
                        if (openStringWriter == null)
                            openStringWriter = openStringWriter ?? writer.OpenString();
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

        private static class Remote {
            public static ExecutionResult Execute(Stream assemblyStream, RuntimeGuardToken guardToken) {
                try {
                    Console.SetOut(Output.Writer);

                    var assembly = Assembly.Load(ReadAllBytes(assemblyStream));
                    var c = assembly.GetType("C");
                    var m = c.GetMethod("M");

                    using (guardToken.Scope()) {
                        var result = m.Invoke(Activator.CreateInstance(c), null);
                        if (m.ReturnType != typeof(void))
                            result.Inspect("Return");
                        return new ExecutionResult(Output.Stream, Flow.Steps);
                    }
                }
                catch (Exception ex) {
                    if (ex is TargetInvocationException invocationEx)
                        ex = invocationEx.InnerException;

                    Flow.ReportException(ex);
                    return new ExecutionResult(ex, Output.Stream, Flow.Steps);
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
