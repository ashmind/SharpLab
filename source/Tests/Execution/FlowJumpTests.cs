using System.Threading.Tasks;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;
using Xunit;
using Xunit.Abstractions;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class FlowJumpTests {
        private readonly ITestOutputHelper _testOutputHelper;

        public FlowJumpTests(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData("Call.InternalAndExternal.cs")]
        [InlineData("Loop.Simple.cs")]
        public async Task Flow_IncludesExpectedJumps(string codeFileName) {
            // Arrange
            var code = await TestCode.FromCodeOnlyFileAsync("Flow/Jumps/" + codeFileName);

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            // Assert
            TestOutput.AssertFlowMatchesJumpComments(code, output, _testOutputHelper);
        }
    }
}
