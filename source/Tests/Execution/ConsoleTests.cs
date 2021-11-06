using System;
using System.Threading.Tasks;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;
using Xunit;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class ConsoleTests {
        [Theory]
        [InlineData("Console.Write(\"abc\");", "abc")]
        [InlineData("Console.WriteLine(\"abc\");", "abc{newline}")]
        [InlineData("Console.Write('a');", "a")]
        [InlineData("Console.Write(3);", "3")]
        [InlineData("Console.Write(3.1);", "3.1")]
        [InlineData("Console.Write(new object());", "System.Object")]
        public async Task Console_IsIncludedInOutput(string consoleCode, string expectedOutput) {// Arrange
            // Arrange
            var code = @"
                using System;
                using System.Globalization;

                public static class Program {
                    public static void Main() {
                        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                        " + consoleCode + @"
                    }
                }
            ";

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            // Assert
            Assert.Equal(
                expectedOutput.Replace("{newline}", Environment.NewLine),
                TestOutput.RemoveFlowJson(output)
            );
        }
    }
}
