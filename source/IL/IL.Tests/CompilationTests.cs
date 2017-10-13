using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using AshMind.Extensions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using Mono.Cecil;
using Xunit;

namespace IL.Tests {
    public class CompilationTests {
        [Theory]
        [InlineData(".class private auto ansi A\r\n{\r\n} // end of class A\r\n")]
        [InlineData(".class public auto ansi A\r\n{\r\n} // end of class A\r\n")]
        [InlineData(".class public auto ansi beforefieldinit A\r\n{\r\n} // end of class A\r\n")]
        public void Declarations(string code) {
            AssertRoundtrips(code);
        }

        private void AssertRoundtrips(string code) {
            Assert.Equal(code, CompileAndDisassemble(code, skipModule: true));
        }

        private static string CompileAndDisassemble(string code, bool skipModule, [CallerMemberName] string callerName = null) {
            var parsed = TestHelper.Parse(code);

            var assemblyStream = new MemoryStream();
            new ILCompiler().Compile(parsed, assemblyStream);
            assemblyStream.Seek(0, SeekOrigin.Begin);

            var assemblyLogPath = Path.Combine(
                Assembly.GetExecutingAssembly().GetAssemblyFileFromCodeBase().DirectoryName,
                $"_CompilationTests.{callerName}.dll"
            );
            File.WriteAllBytes(assemblyLogPath, assemblyStream.ToArray());
            
            var writer = new StringWriter();

            var assembly = AssemblyDefinition.ReadAssembly(assemblyStream);
            var disassembler = new ReflectionDisassembler(new PlainTextOutput(writer), false, CancellationToken.None);
            foreach (var type in assembly.MainModule.Types) {
                if (skipModule && type.Name == "<Module>")
                    continue;
                disassembler.DisassembleType(type);
            }

            return writer.ToString();
        }
    }
}
