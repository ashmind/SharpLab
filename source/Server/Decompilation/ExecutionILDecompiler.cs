using System.IO;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;
using SharpLab.Server.Decompilation.Internal;
using SharpLab.Server.Execution.Internal;

namespace SharpLab.Server.Decompilation {
    public class ExecutionILDecompiler : IDecompiler {
        private readonly IAssemblyStreamRewriterComposer _rewriter;
        private readonly IILDecompiler _ilDecompiler;

        public ExecutionILDecompiler(IAssemblyStreamRewriterComposer rewriter, IILDecompiler ilDecompiler) {
            _rewriter = rewriter;
            _ilDecompiler = ilDecompiler;
        }
        
        public void Decompile(CompilationStreamPair streams, TextWriter codeWriter, IWorkSession session) {
            Argument.NotNull(nameof(streams), streams);
            Argument.NotNull(nameof(codeWriter), codeWriter);

            using var rewritten = _rewriter.Rewrite(streams, session);
            _ilDecompiler.Decompile(rewritten.Stream, symbolStream: null, codeWriter);
        }

        public string LanguageName => TargetNames.RunIL;
    }
}