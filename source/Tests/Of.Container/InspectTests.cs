using SharpLab.Tests.Of.Container.Internal;
using Xunit;

namespace SharpLab.Tests.Of.Container {
    public class InspectTests {
        [Theory]
        [InlineData("3.Inspect();", "#{\"type\":\"inspection:simple\",\"title\":\"Inspect\",\"value\":\"3\"}\n")]
        [InlineData("(1, 2, 3).Inspect();", "#{\"type\":\"inspection:simple\",\"title\":\"Inspect\",\"value\":\"(1, 2, 3)\"}\n")]
        [InlineData("new[] { 1, 2, 3 }.Inspect();", "#{\"type\":\"inspection:simple\",\"title\":\"Inspect\",\"value\":\"{ 1, 2, 3 }\"}\n")]
        [InlineData("3.Dump();", "#{\"type\":\"inspection:simple\",\"title\":\"Dump\",\"value\":\"3\"}\n")]
        public void SimpleInspect_IsIncludedInOutput(string code, string expectedOutput) {
            var output = ContainerTestDriver.CompileAndExecute(code);

            Assert.Equal(expectedOutput, output);
        }
    }
}
