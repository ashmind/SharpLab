using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class FSharpTests {
        [Fact]
        public async Task FSharp_Simple() {
            var code = @"
                open System
                printf ""Test""
            ";

            var output = await ContainerTestDriver.CompileAndExecuteAsync(code, LanguageNames.FSharp);

            Assert.Equal("Test", output);
        }

        [Fact]
        public async Task FSharp_WithExplicitEntryPoint() {
            var code = @"
                open System

                [<EntryPoint>]
                let main argv = 
                    printf ""Test""
                    0
            ";

            var output = await ContainerTestDriver.CompileAndExecuteAsync(code, LanguageNames.FSharp);

            Assert.Equal("Test", output);
        }
    }
}
