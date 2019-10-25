using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.IO;
using MirrorSharp.Advanced;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable;
using Unbreakable.Runtime;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Diagnostics;
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
            using (var definition = AssemblyDefinition.ReadAssembly(streams.AssemblyStream, readerParameters)) {
                AssemblyLog.Log("1.Initial", definition);

                foreach (var rewriter in _rewriters) {
                    rewriter.Rewrite(definition, session);
                }
                PerformanceLog.Checkpoint("Executor.Rewrite.Flow.End");

                AssemblyLog.Log("2.WithFlow", definition);
                if (definition.EntryPoint == null)
                    throw new ArgumentException("Failed to find an entry point (Main?) in assembly.", nameof(streams));

                var guardToken = AssemblyGuard.Rewrite(definition, _guardSettings);
                using (var rewrittenStream = _memoryStreamManager.GetStream()) {
                    definition.Write(rewrittenStream);

                    AssemblyLog.Log("3.Unbreakable", definition);

                    rewrittenStream.Seek(0, SeekOrigin.Begin);
                    PerformanceLog.Checkpoint("Executor.Rewrite.Unbreakable.End");
                    using (var context = new CustomAssemblyLoadContext(shouldShareAssembly: ShouldShareAssembly)) {
                        var assembly = context.LoadFromStream(rewrittenStream);
                        PerformanceLog.Checkpoint("Executor.AssemblyLoad.End");

                        return Execute(assembly, guardToken, session);
                    }
                }
            }
        }

        private bool ShouldShareAssembly(AssemblyName assemblyName) {
            return assemblyName.FullName != typeof(Console).Assembly.FullName;
        }

        public unsafe ExecutionResult Execute(Assembly assembly, RuntimeGuardToken guardToken, IWorkSession session) {
            try {
                Output.Reset();
                Flow.Reset();
                Console.SetOut(Output.Writer);

                var main = assembly.EntryPoint;
                if (main == null)
                    throw new ArgumentException("Entry point not found in " + assembly, nameof(assembly));
                using (guardToken.Scope(NewRuntimeGuardSettings())) {
                    var args = main.GetParameters().Length > 0 ? new object[] { new string[0] } : null;

                    PerformanceLog.Checkpoint("Executor.Invoke.Start");
                    var result = main.Invoke(null, args);
                    PerformanceLog.Checkpoint("Executor.Invoke.End");

                    if (main.ReturnType != typeof(void))
                        result.Inspect("Return");
                    return new ExecutionResult(Output.Stream, Flow.Steps);
                }
            }
            catch (Exception ex) {
                PerformanceLog.Checkpoint("Executor.Invoke.Exception");
                if (ex is TargetInvocationException invocationEx)
                    ex = invocationEx.InnerException ?? ex;

                if (ex is RegexMatchTimeoutException)
                    ex = new TimeGuardException("Time limit reached while evaluating a Regex.\r\nNote that timeout was added by SharpLab — in real code this would not throw, but might run for a very long time.", ex);

                if (ex is StackGuardException sgex)
                    throw new Exception($"{sgex.Message} {sgex.StackBaseline} {sgex.StackOffset} {sgex.StackLimit} {sgex.StackSize}");

                Flow.ReportException(ex);
                Output.Write(new SimpleInspection("Exception", ex.ToString()));
                if (ShouldMonitorException(ex))
                    _monitor.Exception(ex!, session);
                return new ExecutionResult(Output.Stream, Flow.Steps);
            }
        }

        private static RuntimeGuardSettings NewRuntimeGuardSettings() {
            #if DEBUG
            if (Debugger.IsAttached)
                return new RuntimeGuardSettings { TimeLimit = TimeSpan.MaxValue };
            #endif
            return new RuntimeGuardSettings { TimeLimit = TimeSpan.FromSeconds(1) };
        }

        private static bool ShouldMonitorException(Exception? exception) {
            return exception is GuardException
                || exception is InvalidProgramException;
        }

        public void Serialize(ExecutionResult result, IFastJsonWriter writer) {
            _serializer.Serialize(result, writer);
        }

        private static AssemblyGuardSettings CreateGuardSettings(ApiPolicy apiPolicy) {
            var settings = AssemblyGuardSettings.DefaultForCSharpAssembly();
            settings.ApiPolicy = apiPolicy;

            Regex? CompileRegex(Regex? original) => original != null
                ? new Regex(original.ToString(), original.Options | RegexOptions.Compiled)
                : null;
            settings.AllowExplicitLayoutInTypesMatchingPattern = CompileRegex(settings.AllowExplicitLayoutInTypesMatchingPattern);
            settings.AllowPointerOperationsInTypesMatchingPattern = CompileRegex(settings.AllowPointerOperationsInTypesMatchingPattern);

            return settings;
        }
    }
}