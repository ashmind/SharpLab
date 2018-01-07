using System.IO;
using System.Threading;
using ICSharpCode.Decompiler.Disassembler;
using Mono.Cecil;
using SharpLab.Server.Common;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    public class ILDecompiler : IDecompiler {
        public void Decompile(Stream assemblyStream, TextWriter codeWriter) {
            var assembly = AssemblyDefinition.ReadAssembly(assemblyStream);
            //#if DEBUG
            //assembly.Write(@"d:\Temp\assembly\" + System.DateTime.Now.Ticks + "-il.dll");
            //#endif

            var output = new CustomizableIndentPlainTextOutput(codeWriter) {
                IndentationString = "    "
            };
            var disassembler = new ReflectionDisassembler(output, false, CancellationToken.None);
            disassembler.WriteModuleContents(assembly.MainModule);
        }

        public string LanguageName => TargetNames.IL;
    }
}