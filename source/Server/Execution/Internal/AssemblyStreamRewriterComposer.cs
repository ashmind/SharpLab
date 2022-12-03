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

            var module = ModuleDefinition.ReadModule(streams.AssemblyStream, readerParameters);
            try {
                if (module.Assembly is {} assembly && HasNoRewritingAttribute(assembly)) {
                    streams.AssemblyStream.Position = 0;
                    return new(streams.AssemblyStream, null, module);
                }

                return RewriteInternal(module, session, rewriteStopwatch);
            }
            catch {
                module.Dispose();
                throw;
            }
        }

        private AssemblyStreamRewriteResult RewriteInternal(ModuleDefinition module, IWorkSession session, Stopwatch? rewriteStopwatch) {
            foreach (var rewriter in _rewriters) {
                rewriter.Rewrite(module, session);
            }
            
            #if DEBUG
            DiagnosticLog.LogAssembly("2.WithFlow", module);
            #endif

            var rewrittenStream = _memoryStreamManager.GetStream();
            try {
                module.Write(rewrittenStream);
                rewrittenStream.Position = 0;
                rewriteStopwatch?.Stop();
                return new(rewrittenStream, rewriteStopwatch?.Elapsed, module);
            }
            catch {
                rewrittenStream.Dispose();
                throw;
            }
        }

        private bool HasNoRewritingAttribute(AssemblyDefinition assembly) {
            if (!assembly.HasCustomAttributes)
                return false;

            foreach (var attribute in assembly.CustomAttributes) {
                if (attribute.AttributeType.Name == nameof(NoILRewritingAttribute))
                    return true;
            }

            return false;
        }
    }
}
