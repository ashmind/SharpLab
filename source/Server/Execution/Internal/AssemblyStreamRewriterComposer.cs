using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.IO;
using MirrorSharp.Advanced;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SharpLab.Runtime;
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

        public AssemblyStreamRewriteResult Rewrite(CompilationStreamPair streams, IWorkSession session) {
            var includePerformance = session.ShouldReportPerformance();
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

                return RewriteInternal(definition, session, rewriteStopwatch);
            }
            catch {
                definition.Dispose();
                throw;
            }
        }

        private AssemblyStreamRewriteResult RewriteInternal(AssemblyDefinition definition, IWorkSession session, Stopwatch? rewriteStopwatch) {
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

        private bool HasNoRewritingAttribute(AssemblyDefinition definition) {
            if (!definition.HasCustomAttributes)
                return false;

            foreach (var attribute in definition.CustomAttributes) {
                if (attribute.AttributeType.Name == nameof(NoILRewritingAttribute))
                    return true;
            }

            return false;
        }
    }
}
