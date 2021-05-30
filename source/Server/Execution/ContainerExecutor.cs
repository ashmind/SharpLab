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
using SharpLab.Server.Common;
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

        public async Task<string> ExecuteAsync(CompilationStreamPair streams, IWorkSession session, CancellationToken cancellationToken) {
            if (!session.IsContainerExperimentAllowed())
                throw new UnauthorizedAccessException("Current session is not allowed access to container experiment.");

            var includePerformance = session.ShouldReportPerformance();
            var rewriteStopwatch = includePerformance ? Stopwatch.StartNew() : null;
            var readerParameters = new ReaderParameters {
                ReadSymbols = streams.SymbolStream != null,
                SymbolStream = streams.SymbolStream,
                AssemblyResolver = _assemblyResolver,
                SymbolReaderProvider = streams.SymbolStream != null ? _symbolReaderProvider : null
            };

            using var _ = streams;
            using var definition = AssemblyDefinition.ReadAssembly(streams.AssemblyStream, readerParameters);

            foreach (var rewriter in _rewriters) {
                rewriter.Rewrite(definition, session);
            }

            if (definition.EntryPoint == null)
                throw new ArgumentException("Failed to find an entry point (Main?) in assembly.", nameof(streams));

            using var rewrittenStream = _memoryStreamManager.GetStream();
            definition.Write(rewrittenStream);
            rewrittenStream.Seek(0, SeekOrigin.Begin);
            rewriteStopwatch?.Stop();

            var executeStopwatch = includePerformance ? Stopwatch.StartNew() : null;
            var output = await _client.ExecuteAsync(session.GetSessionId(), rewrittenStream, includePerformance, cancellationToken);
            if (rewriteStopwatch != null && executeStopwatch != null) {
                // TODO: Prettify
                output += $"\n  REWRITERS: {rewriteStopwatch.ElapsedMilliseconds,17}ms\n  CONTAINER EXECUTOR: {executeStopwatch.ElapsedMilliseconds,8}ms";
            }
            return output;
        }
    }
}