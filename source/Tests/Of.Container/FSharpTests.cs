using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SharpLab.Tests.Internal;
using SharpLab.Tests.Of.Container.Internal;
using Xunit;

namespace SharpLab.Tests.Of.Container {
    [Collection(TestCollectionNames.Execution)]
    public class FSharpTests {
        [Fact]
        public async Task SlowUpdate_ExecutesFSharp() {
            var code = @"
                open System
                printf ""Test""
            ";

            var output = await ContainerTestDriver.CompileAndExecuteAsync(code, LanguageNames.FSharp);

            Assert.Equal("Test", output);
        }

        [Fact]
        public async Task SlowUpdate_ExecutesFSharp_WithExplicitEntryPoint() {
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
