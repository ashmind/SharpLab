using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.IO;
using MirrorSharp.Advanced;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Diagnostics;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Execution.Internal {
    public class AssemblyStreamRewriterComposer : IAssemblyStreamRewriterComposer {
        private readonly IAssemblyResolver _assemblyResolver;
        private readonly ISymbolReaderProvider _symbolReaderProvider;
        private readonly IReadOnlyCollection<IAssemblyRewriter> _rewriters;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public AssemblyStreamRewriterComposer(
            IAssemblyResolver assemblyResolver,
            ISymbolReaderProvider symbolReaderProvider,
            IReadOnlyCollection<IAssemblyRewriter> rewriters,
            RecyclableMemoryStreamManager memoryStreamManager
        ) {
            _assemblyResolver = assemblyResolver;
            _symbolReaderProvider = symbolReaderProvider;
            _rewriters = rewriters;
            _memoryStreamManager = memoryStreamManager;
        }

        public Stream Rewrite(CompilationStreamPair streams, IWorkSession session) {
            var includePerformance = session.ShouldReportPerformance();
            var rewriteStopwatch = includePerformance ? Stopwatch.StartNew() : null;
            var readerParameters = new ReaderParameters {
                ReadSymbols = streams.SymbolStream != null,
                SymbolStream = streams.SymbolStream,
                AssemblyResolver = _assemblyResolver,
                SymbolReaderProvider = streams.SymbolStream != null ? _symbolReaderProvider : null
            };

            using var definition = AssemblyDefinition.ReadAssembly(streams.AssemblyStream, readerParameters);

            foreach (var rewriter in _rewriters) {
                rewriter.Rewrite(definition, session);
            }

            #if DEBUG
            DiagnosticLog.LogAssembly("2.WithFlow", definition);
            #endif

            var rewrittenStream = _memoryStreamManager.GetStream();
            try {
                definition.Write(rewrittenStream);
                rewrittenStream.Seek(0, SeekOrigin.Begin);
                rewriteStopwatch?.Stop();
            }
            catch (Exception) {
                rewrittenStream.Dispose();
                throw;
            }
            return rewrittenStream;
        }
    }
}
