using System.IO;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SharpLab.Server.Common;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    public class ILDecompiler : IDecompiler {
        private readonly ISymbolReaderProvider _symbolReaderProvider;

        public ILDecompiler(ISymbolReaderProvider symbolReaderProvider) {
            _symbolReaderProvider = symbolReaderProvider;
        }

        public void Decompile(CompilationStreamPair streams, TextWriter codeWriter) {
            Argument.NotNull(nameof(streams), streams);
            Argument.NotNull(nameof(codeWriter), codeWriter);

            var assembly = AssemblyDefinition.ReadAssembly(streams.AssemblyStream, new ReaderParameters {
                ReadSymbols = streams.SymbolStream != null,
                SymbolStream = streams.SymbolStream,
                SymbolReaderProvider = streams.SymbolStream != null ? _symbolReaderProvider : null
            });
            //#if DEBUG
            //assembly.Write(@"d:\Temp\assembly\" + System.DateTime.Now.Ticks + "-il.dll");
            //#endif

            var output = new SpaceIndentingPlainTextOutput(codeWriter);
            var disassembler = new ReflectionDisassembler(output, CancellationToken.None) {
                ShowSequencePoints = true
            };
            disassembler.WriteModuleContents(assembly.MainModule);
        }

        public string LanguageName => TargetNames.IL;
    }
}