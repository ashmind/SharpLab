using System;
using System.IO;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using Mono.Cecil.Cil;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Diagnostics;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    public class ILDecompiler : IDecompiler {
        private readonly ISymbolReaderProvider _symbolReaderProvider;
        private readonly Func<Stream, IDisposableDebugInfoProvider> _debugInfoFactory;

        public ILDecompiler(ISymbolReaderProvider symbolReaderProvider, Func<Stream, IDisposableDebugInfoProvider> debugInfoFactory) {
            _symbolReaderProvider = symbolReaderProvider;
            _debugInfoFactory = debugInfoFactory;
        }

        public void Decompile(CompilationStreamPair streams, TextWriter codeWriter) {
            Argument.NotNull(nameof(streams), streams);
            Argument.NotNull(nameof(codeWriter), codeWriter);

            using (var assemblyFile = new PEFile("", streams.AssemblyStream))
            using (var debugInfo = streams.SymbolStream != null ? _debugInfoFactory(streams.SymbolStream) : null) {
                var output = new PlainTextOutput(codeWriter) { IndentationString = "    " };
                var disassembler = new ReflectionDisassembler(output, CancellationToken.None) {
                    DebugInfo = debugInfo,
                    ShowSequencePoints = true
                };
                disassembler.WriteModuleContents(assemblyFile);
            }
        }

        public string LanguageName => TargetNames.IL;
    }
}