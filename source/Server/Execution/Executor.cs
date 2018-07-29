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
using SharpLab.Server.Monitoring;
using IAssemblyResolver = Mono.Cecil.IAssemblyResolver;

namespace SharpLab.Server.Execution {
    public class Executor : IExecutor {
        private readonly IAssemblyResolver _assemblyResolver;
        private readonly ISymbolReaderProvider _symbolReaderProvider;
        private readonly AssemblyGuardSettings _guardSettings;
        private readonly IReadOnlyCollection<IAssemblyRewriter> _rewriters;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;
        private readonly ExecutionResultSerializer _serializer;
        private readonly IMonitor _monitor;

        public Executor(
            IAssemblyResolver assemblyResolver,
            ISymbolReaderProvider symbolReaderProvider,
            ApiPolicy apiPolicy,
            IReadOnlyCollection<IAssemblyRewriter> rewriters,
            RecyclableMemoryStreamManager memoryStreamManager,
            ExecutionResultSerializer serializer,
            IMonitor monitor
        ) {
            _assemblyResolver = assemblyResolver;
            _symbolReaderProvider = symbolReaderProvider;
            _guardSettings = CreateGuardSettings(apiPolicy);
            _rewriters = rewriters;
            _memoryStreamManager = memoryStreamManager;
            _serializer = serializer;
            _monitor = monitor;
        }

        public ExecutionResult Execute(CompilationStreamPair streams, IWorkSession session) {
            var readerParameters = new ReaderParameters {
                ReadSymbols = streams.SymbolStream != null,
                SymbolStream = streams.SymbolStream,
                AssemblyResolver = _assemblyResolver,
                SymbolReaderProvider = streams.SymbolStream != null ? _symbolReaderProvider : null
            };

            using (streams)
            using (var assembly = AssemblyDefinition.ReadAssembly(streams.AssemblyStream, readerParameters)) {
                /*
                #if DEBUG
                assembly.Write(@"d:\Temp\assembly\" + DateTime.Now.Ticks + "-before-rewrite.dll");
                #endif
                */
                foreach (var rewriter in _rewriters) {
                    rewriter.Rewrite(assembly, session);
                }
                if (assembly.EntryPoint == null)
                    throw new ArgumentException("Failed to find an entry point (Main?) in assembly.", nameof(streams));

                var guardToken = AssemblyGuard.Rewrite(assembly, _guardSettings);
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
            _serializer.Serialize(result, writer);
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

                    if (ex is RegexMatchTimeoutException)
                        ex = new TimeGuardException("Time limit reached while evaluating a Regex.\r\nNote that timeout was added by SharpLab — in real code this would not throw, but might run for a very long time.", ex);

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

        private static AssemblyGuardSettings CreateGuardSettings(ApiPolicy apiPolicy) {
            var settings = AssemblyGuardSettings.DefaultForCSharpAssembly();
            settings.ApiPolicy = apiPolicy;
            settings.AllowExplicitLayoutInTypesMatchingPattern = new Regex(settings.AllowExplicitLayoutInTypesMatchingPattern.ToString(), RegexOptions.Compiled);
            settings.AllowPointerOperationsInTypesMatchingPattern = new Regex(settings.AllowPointerOperationsInTypesMatchingPattern.ToString(), RegexOptions.Compiled);
            settings.AllowCustomTypesMatchingPatternInSystemNamespaces = new Regex(
                settings.AllowCustomTypesMatchingPatternInSystemNamespaces.ToString() + @"|System\.Range|System\.Index|System\.Extensions", RegexOptions.Compiled
            );
            return settings;
        }
    }
}
