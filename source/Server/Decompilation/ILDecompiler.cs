using System;
using System.IO;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    public class ILDecompiler : IILDecompiler {
        private readonly Func<Stream, IDisposableDebugInfoProvider> _debugInfoFactory;

        public ILDecompiler(Func<Stream, IDisposableDebugInfoProvider> debugInfoFactory) {
            _debugInfoFactory = debugInfoFactory;
        }

        public void Decompile(CompilationStreamPair streams, TextWriter codeWriter, IWorkSession session) {
            Argument.NotNull(nameof(streams), streams);
            Argument.NotNull(nameof(codeWriter), codeWriter);
            Argument.NotNull(nameof(session), session);

            Decompile(streams.AssemblyStream, streams.SymbolStream, codeWriter);
        }

        public void Decompile(Stream assemblyStream, Stream? symbolStream, TextWriter codeWriter) {
            Argument.NotNull(nameof(assemblyStream), assemblyStream);
            Argument.NotNull(nameof(codeWriter), codeWriter);

            using var assemblyFile = new PEFile("_", assemblyStream);
            using var debugInfo = symbolStream != null ? _debugInfoFactory(symbolStream) : null;

            var output = new PlainTextOutput(codeWriter) { IndentationString = "    " };
            var disassembler = new ReflectionDisassembler(output, CancellationToken.None) {
                DebugInfo = debugInfo,
                ShowSequencePoints = true
            };

            disassembler.WriteAssemblyHeader(assemblyFile);
            output.WriteLine(); // empty line
            disassembler.WriteModuleContents(assemblyFile);
        }

        public string LanguageName => TargetNames.IL;
    }
}