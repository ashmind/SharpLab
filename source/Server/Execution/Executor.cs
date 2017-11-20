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
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common;
using SharpLab.Server.Execution.Internal;
using SharpLab.Server.Monitoring;
using Unbreakable;
using Unbreakable.Runtime;

namespace SharpLab.Server.Execution {
    public class Executor : IExecutor {
        private readonly IReadOnlyCollection<IAssemblyRewriter> _rewriters;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;
        private readonly IMonitor _monitor;

        public Executor(IReadOnlyCollection<IAssemblyRewriter> rewriters, RecyclableMemoryStreamManager memoryStreamManager, IMonitor monitor) {
            _rewriters = rewriters;
            _memoryStreamManager = memoryStreamManager;
            _monitor = monitor;
        }

        public ExecutionResult Execute(Stream assemblyStream, Stream symbolStream, IWorkSession session) {
            AssemblyDefinition assembly;
            using (assemblyStream)
            using (symbolStream) {
                assembly = AssemblyDefinition.ReadAssembly(assemblyStream, new ReaderParameters {
                    ReadSymbols = true,
                    SymbolStream = symbolStream,
                    AssemblyResolver = PreCachedAssemblyResolver.Instance
                });
            }
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

                var currentSetup = AppDomain.CurrentDomain.SetupInformation;
                using (var context = AppDomainContext.Create(new AppDomainSetup {
                    ApplicationBase = currentSetup.ApplicationBase,
                    PrivateBinPath = currentSetup.PrivateBinPath
                })) {
                    context.LoadAssembly(LoadMethod.LoadFrom, Assembly.GetExecutingAssembly().GetAssemblyFile().FullName);
                    var (result, exception) = RemoteFunc.Invoke(context.Domain, rewrittenStream, guardToken, Remote.Execute);
                    if (ShouldMonitorException(exception))
                        _monitor.Exception(exception, session);
                    return result;
                }
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
            public static ExecutionResultWrapper Execute(Stream assemblyStream, RuntimeGuardToken guardToken) {
                try {
                    Console.SetOut(Output.Writer);

                    var assembly = Assembly.Load(ReadAllBytes(assemblyStream));
                    var main = assembly.EntryPoint;
                    using (guardToken.Scope(NewRuntimeGuardSettings())) {
                        var args = main.GetParameters().Length > 0 ? new object[] { new string[0] } : null;
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
