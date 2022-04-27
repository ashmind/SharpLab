using System.Threading.Tasks;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;
using Xunit;
using Xunit.Abstractions;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class CompilationTests {
        public CompilationTests(ITestOutputHelper output) {
            //TestAssemblyLog.Enable(output);
        }

        [Theory]
        [InlineData("UnsafeKeyword.cs")]
        public async Task Compilation_IsIncludedInOutput(string codeFileName) {
            // Arrange
            var code = await TestCode.FromCodeOnlyFileAsync("Compilation/" + codeFileName);

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            // Assert
            Assert.DoesNotMatch("Exception:", output);
        }
    }
}
