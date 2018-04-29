using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using AppDomainToolkit;
using AshMind.Extensions;
using Microsoft.IO;
using MirrorSharp.Advanced;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable;
using Unbreakable.Runtime;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common;
using SharpLab.Server.Execution.Internal;
using SharpLab.Server.Execution.Unbreakable;
using SharpLab.Server.Monitoring;
using IAssemblyResolver = Mono.Cecil.IAssemblyResolver;
using System.Text;

namespace SharpLab.Server.Execution {
    public class Executor : IExecutor {
        private readonly IAssemblyResolver _assemblyResolver;
        private readonly ISymbolReaderProvider _symbolReaderProvider;
        private readonly IReadOnlyCollection<IAssemblyRewriter> _rewriters;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;
        private readonly IMonitor _monitor;

        public Executor(IAssemblyResolver assemblyResolver, ISymbolReaderProvider symbolReaderProvider, IReadOnlyCollection<IAssemblyRewriter> rewriters, RecyclableMemoryStreamManager memoryStreamManager, IMonitor monitor) {
            _assemblyResolver = assemblyResolver;
            _symbolReaderProvider = symbolReaderProvider;
            _rewriters = rewriters;
            _memoryStreamManager = memoryStreamManager;
            _monitor = monitor;
        }

        public ExecutionResult Execute(Stream assemblyStream, Stream symbolStream, IWorkSession session) {
            var readerParameters = new ReaderParameters {
                ReadSymbols = symbolStream != null,
                SymbolStream = symbolStream,
                AssemblyResolver = _assemblyResolver,
                SymbolReaderProvider = symbolStream != null ? _symbolReaderProvider : null
            };

            using (assemblyStream)
            using (symbolStream)
            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyStream, readerParameters)) {
                /*
                #if DEBUG
                assembly.Write(@"d:\Temp\assembly\" + DateTime.Now.Ticks + "-before-rewrite.dll");
                #endif
                */
                foreach (var rewriter in _rewriters) {
                    rewriter.Rewrite(assembly, session);
                }
                if (assembly.EntryPoint == null)
                    throw new ArgumentException("Failed to find an entry point (Main?) in assembly.", nameof(assemblyStream));

                var guardToken = AssemblyGuard.Rewrite(assembly, GuardSettings);
                using (var rewrittenStream = _memoryStreamManager.GetStream()) {
                    assembly.Write(rewrittenStream);
                    /*
                    #if DEBUG
                    assembly.Write(@"d:\Temp\assembly\" + DateTime.Now.Ticks + ".dll");
                    #endif
                    */
                    rewrittenStream.Seek(0, SeekOrigin.Begin);

                    return ExecuteInAppDomain(rewrittenStream, guardToken, session);
                }
            }
        }

        private ExecutionResult ExecuteInAppDomain(MemoryStream assemblyStream, RuntimeGuardToken guardToken, IWorkSession session) {
            var currentSetup = AppDomain.CurrentDomain.SetupInformation;
            using (var context = AppDomainContext.Create(new AppDomainSetup {
                ApplicationBase = currentSetup.ApplicationBase,
                PrivateBinPath = currentSetup.PrivateBinPath
            })) {
                context.LoadAssembly(LoadMethod.LoadFrom, Assembly.GetExecutingAssembly().GetAssemblyFile().FullName);
                var (result, exception) = RemoteFunc.Invoke(context.Domain, assemblyStream, guardToken, CurrentProcess.Id, Remote.Execute);
                if (ShouldMonitorException(exception))
                    _monitor.Exception(exception, session);
                return result;
            }
        }

        private static bool ShouldMonitorException(Exception exception) {
            return exception is GuardException
                || exception is InvalidProgramException;
        }

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
                writer.WriteStartObject();
                writer.WriteProperty("name", label.Name);
                writer.WriteProperty("offset", label.Offset);
                writer.WriteProperty("length", label.Length);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyStartArray("data");
            foreach (var @byte in memory.Data) {
                writer.WriteValue(@byte);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private static class Remote {
            public static unsafe ExecutionResultWrapper Execute(Stream assemblyStream, RuntimeGuardToken guardToken, int processId) {
                try {
                    Console.SetOut(Output.Writer);
                    InspectionSettings.CurrentProcessId = processId;

                    var assembly = Assembly.Load(ReadAllBytes(assemblyStream));
                    var main = assembly.EntryPoint;
                    using (guardToken.Scope(NewRuntimeGuardSettings())) {
                        var args = main.GetParameters().Length > 0 ? new object[] { new string[0] } : null;
                        byte* stackStart = stackalloc byte[1];
                        InspectionSettings.StackStart = (ulong)stackStart;
                        var result = main.Invoke(null, args);
                        if (main.ReturnType != typeof(void))
                            result.Inspect("Return");
                        return new ExecutionResultWrapper(new ExecutionResult(Output.Stream, Flow.Steps), null);
                    }
                }
                catch (Exception ex) {
                    if (ex is TargetInvocationException invocationEx)
                        ex = invocationEx.InnerException;

                    Flow.ReportException(ex);
                    ex.Inspect("Exception");
                    return new ExecutionResultWrapper(new ExecutionResult(Output.Stream, Flow.Steps), ex);
                }
            }

            private static RuntimeGuardSettings NewRuntimeGuardSettings() {
                #if DEBUG
                if (Debugger.IsAttached)
                    return new RuntimeGuardSettings { TimeLimit = TimeSpan.MaxValue };
                #endif
                return null;
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

            [Serializable]
            public struct ExecutionResultWrapper {
                public ExecutionResultWrapper(ExecutionResult result, Exception exception = null) {
                    Result = result;
                    Exception = exception;
                }

                public void Deconstruct(out ExecutionResult result, out Exception exception) {
                    result = Result;
                    exception = Exception;
                }

                public ExecutionResult Result { get; }
                public Exception Exception { get; }
            }
        }

        private static readonly AssemblyGuardSettings GuardSettings = ((Func<AssemblyGuardSettings>)(() => {
            var settings = AssemblyGuardSettings.DefaultForCSharpAssembly();
            settings.ApiPolicy = ApiPolicySetup.CreatePolicy();
            settings.AllowExplicitLayoutInTypesMatchingPattern = new Regex(settings.AllowExplicitLayoutInTypesMatchingPattern.ToString(), RegexOptions.Compiled);
            settings.AllowPointerOperationsInTypesMatchingPattern = new Regex(settings.AllowPointerOperationsInTypesMatchingPattern.ToString(), RegexOptions.Compiled);
            settings.AllowCustomTypesMatchingPatternInSystemNamespaces = new Regex(
                settings.AllowCustomTypesMatchingPatternInSystemNamespaces.ToString() + @"|System\.Range", RegexOptions.Compiled
            );
            return settings;
        }))();
    }
}
