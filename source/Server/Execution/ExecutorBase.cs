using System;
using System.Collections.Generic;
using System.IO;
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
using SharpLab.Server.Execution;

namespace SharpLab.Server.AspNetCore.Execution {
    public abstract class ExecutorBase : IExecutor {
        private readonly IAssemblyResolver _assemblyResolver;
        private readonly ISymbolReaderProvider _symbolReaderProvider;
        private readonly AssemblyGuardSettings _guardSettings;
        private readonly IReadOnlyCollection<IAssemblyRewriter> _rewriters;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;
        private readonly ExecutionResultSerializer _serializer;
        private readonly IMonitor _monitor;

        public ExecutorBase(
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

        protected abstract ExecutionResultWithException ExecuteWithIsolation(MemoryStream assemblyStream, RuntimeGuardToken guardToken, IWorkSession session) ;
        
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