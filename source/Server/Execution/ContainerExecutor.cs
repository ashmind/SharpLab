using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;
using MirrorSharp.Advanced;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SharpLab.Runtime;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Diagnostics;
using SharpLab.Server.Execution.Container;
using SharpLab.Server.Execution.Internal;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Execution {
    public class ContainerExecutor : IContainerExecutor {
        private readonly IAssemblyResolver _assemblyResolver;
        private readonly ISymbolReaderProvider _symbolReaderProvider;
        private readonly IReadOnlyCollection<IContainerAssemblyRewriter> _rewriters;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;
        private readonly IContainerClient _client;

        public ContainerExecutor(
            IAssemblyResolver assemblyResolver,
            ISymbolReaderProvider symbolReaderProvider,
            IReadOnlyCollection<IContainerAssemblyRewriter> rewriters,
            RecyclableMemoryStreamManager memoryStreamManager,
            IContainerClient client
        ) {
            _assemblyResolver = assemblyResolver;
            _symbolReaderProvider = symbolReaderProvider;
            _rewriters = rewriters;
            _memoryStreamManager = memoryStreamManager;
            _client = client;
        }

        public async Task<ContainerExecutionResult> ExecuteAsync(CompilationStreamPair streams, IWorkSession session, CancellationToken cancellationToken) {
            var includePerformance = session.ShouldReportPerformance();
            using var rewritten = RewriteAssembly(streams, session, includePerformance);

            var executeStopwatch = includePerformance ? Stopwatch.StartNew() : null;
            var result = await _client.ExecuteAsync(session.GetSessionId(), rewritten.Stream, includePerformance, cancellationToken);
            if (rewritten.ElapsedTime != null && executeStopwatch != null) {
                // TODO: Prettify
                // output += $"\n  REWRITERS: {rewriteStopwatch.ElapsedMilliseconds,17}ms\n  CONTAINER EXECUTOR: {executeStopwatch.ElapsedMilliseconds,8}ms";
            }
            return result;
        }

        private RewriteResult RewriteAssembly(CompilationStreamPair streams, IWorkSession session, bool includePerformance) {
            var rewriteStopwatch = includePerformance ? Stopwatch.StartNew() : null;
            var readerParameters = new ReaderParameters {
                ReadSymbols = streams.SymbolStream != null,
                SymbolStream = streams.SymbolStream,
                AssemblyResolver = _assemblyResolver,
                SymbolReaderProvider = streams.SymbolStream != null ? _symbolReaderProvider : null
            };
            var definition = AssemblyDefinition.ReadAssembly(streams.AssemblyStream, readerParameters);
            try {
                if (HasNoRewritingAttribute(definition)) {
                    streams.AssemblyStream.Position = 0;
                    return new(streams.AssemblyStream, null, definition);
                }

                foreach (var rewriter in _rewriters) {
                    rewriter.Rewrite(definition, session);
                }

                #if DEBUG
                DiagnosticLog.LogAssembly("2.WithFlow", definition);
                #endif

                var rewrittenStream = _memoryStreamManager.GetStream();
                try {
                    definition.Write(rewrittenStream);
                    rewrittenStream.Position = 0;
                    rewriteStopwatch?.Stop();
                    return new(rewrittenStream, rewriteStopwatch?.Elapsed, definition);
                }
                catch {
                    rewrittenStream.Dispose();
                    throw;
                }
            }
            catch {
                definition.Dispose();
                throw;
            }
        }

        private bool HasNoRewritingAttribute(AssemblyDefinition definition) {
            if (!definition.HasCustomAttributes)
                return false;

            foreach (var attribute in definition.CustomAttributes) {
                if (attribute.AttributeType.Name == nameof(NoILRewritingAttribute))
                    return true;
            }

            return false;
        }

        private readonly struct RewriteResult : IDisposable {
            private readonly AssemblyDefinition _assembly;

            public RewriteResult(Stream stream, TimeSpan? elapsedTime, AssemblyDefinition assembly) {
                Stream = stream;
                ElapsedTime = elapsedTime;
                _assembly = assembly;
            }

            public Stream Stream { get; }
            public TimeSpan? ElapsedTime { get; }

            public void Dispose() {
                var assemblyDisposeException = (Exception?)null;
                try {
                    _assembly.Dispose();
                }
                catch (Exception ex) {
                    assemblyDisposeException = ex;
                }
                
                try {
                    Stream.Dispose();
                }
                catch (Exception ex) when (assemblyDisposeException != null) {
                    throw new AggregateException(assemblyDisposeException, ex);
                }
            }
        }
    }
}