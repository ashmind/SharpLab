using System.Threading.Tasks;
using Xunit;
using SharpLab.Server.Common;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class ILTests {
        [Fact]
        public async Task IL_Simple() {
            var code = @"
                .assembly ConsoleApp
                {
                }

                .class private auto ansi abstract sealed beforefieldinit Program
                    extends [System.Private.CoreLib]System.Object
                {
                    .method private hidebysig static void Main (string[] args) cil managed 
                    {
                        .maxstack 8
                        .entrypoint

                        ldstr ""🌄""
                        call void [System.Console]System.Console::Write(string)
                        ret
                    }
                }
            ";

            var output = await ContainerTestDriver.CompileAndExecuteAsync(code, LanguageNames.IL);

            Assert.Equal("🌄", output);
        }
    }
}
