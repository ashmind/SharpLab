using System.Threading.Tasks;
using Xunit;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class InspectValueTests {
        [Theory]
        [InlineData("3.Inspect();", "#{\"type\":\"inspection:simple\",\"title\":\"Inspect\",\"value\":\"3\"}\n")]
        [InlineData("(1, 2, 3).Inspect();", "#{\"type\":\"inspection:simple\",\"title\":\"Inspect\",\"value\":\"(1, 2, 3)\"}\n")]
        [InlineData("new[] { 1, 2, 3 }.Inspect();", "#{\"type\":\"inspection:simple\",\"title\":\"Inspect\",\"value\":\"{ 1, 2, 3 }\"}\n")]
        [InlineData("3.Dump();", "#{\"type\":\"inspection:simple\",\"title\":\"Dump\",\"value\":\"3\"}\n")]
        public async Task Inspect_ProducesExpectedOutput(string code, string expectedOutput) {
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            Assert.Equal(expectedOutput, TestOutput.RemoveFlowJson(output));
        }
    }
}
