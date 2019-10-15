using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Microsoft.IO;
using MirrorSharp.Advanced;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable;
using Unbreakable.Runtime;
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
            using (var definition = AssemblyDefinition.ReadAssembly(streams.AssemblyStream, readerParameters)) {
                AssemblyLog.Log("1.Initial", definition);

                foreach (var rewriter in _rewriters) {
                    rewriter.Rewrite(definition, session);
                }
                AssemblyLog.Log("2.WithFlow", definition);
                if (definition.EntryPoint == null)
                    throw new ArgumentException("Failed to find an entry point (Main?) in assembly.", nameof(streams));

                var guardToken = AssemblyGuard.Rewrite(definition, _guardSettings);
                using (var rewrittenStream = _memoryStreamManager.GetStream()) {
                    definition.Write(rewrittenStream);
                    AssemblyLog.Log("3.Unbreakable", definition);

                    rewrittenStream.Seek(0, SeekOrigin.Begin);
                    var (result, exception) = ExecuteWithIsolation(rewrittenStream, guardToken, session);
                    if (ShouldMonitorException(exception))
                        _monitor.Exception(exception!, session);

                    return result;
                }
            }
        }

        private ExecutionResultWithException ExecuteWithIsolation(MemoryStream assemblyStream, RuntimeGuardToken guardToken, IWorkSession session) {
            using (var context = new CustomAssemblyLoadContext(shouldShareAssembly: _ => false)) {
                var assembly = context.LoadFromStream(assemblyStream);
                var serverAssembly = context.LoadFromAssemblyPath(Current.AssemblyPath);

                var coreType = serverAssembly.GetType(typeof(IsolatedExecutorCore).FullName!)!;
                var execute = coreType.GetMethod(nameof(IsolatedExecutorCore.Execute))!;

                var wrapperInContext = execute.Invoke(null, new object[] { assembly, guardToken.Guid, Current.ProcessId, ProfilerState.Active });
                // Since wrapperInContext belongs to a different AssemblyLoadContext, it is not possible to convert
                // it to same type in the default context without some trick (e.g. serialization).
                using (var wrapperStream = _memoryStreamManager.GetStream()) {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(wrapperStream, wrapperInContext);
                    wrapperStream.Seek(0, SeekOrigin.Begin);
                    return (ExecutionResultWithException)formatter.Deserialize(wrapperStream);
                }
            }
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