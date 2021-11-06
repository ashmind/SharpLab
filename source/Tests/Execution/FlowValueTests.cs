using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class FlowValueTests {
        private readonly ITestOutputHelper _testOutputHelper;

        public FlowValueTests(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData("Variable.AssignCall.cs")]
        [InlineData("Variable.ManyVariables.cs")]
        [InlineData("Return.Simple.cs")]
        [InlineData("Return.Ref.cs")]
        [InlineData("Return.Ref.Readonly.cs")]
        [InlineData("Loop.For.10Iterations.cs")]
        [InlineData("Variable.MultipleDeclarationsOnTheSameLine.cs")]
        [InlineData("Variable.LongName.cs")]
        [InlineData("Variable.LongValue.cs")]
        [InlineData("Variable.LongValue.UnicodeCharBreak.cs")]
        [InlineData("Regression.ToStringNull.cs")] // https://github.com/ashmind/SharpLab/issues/380
        [InlineData("Variable.Array.cs")]
        public async Task Flow_IncludesExpectedValues(string codeFileName) {
            // Arrange
            var code = await TestCode.FromCodeOnlyFileAsync("Flow/Values/" + codeFileName);

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            // Assert
            TestOutput.AssertFlowMatchesComments(code, output, _testOutputHelper);
        }

        [Theory]
        [InlineData("void M(int a) {} // [a: 1]", "M(1);")]
        [InlineData("void M(int a) { // [a: 1]\r\n}", "M(1);")]
        //[InlineData("void M(int a)\r\n{}", "M(1)", "a: 1", true)]
        //[InlineData("void M(int a\r\n) {}", "M(1)", "a: 1", true)]
        //[InlineData("void M(\r\nint a\r\n) {}", "M(1)", 2, "a: 1", true)]
        [InlineData("void M(int a) {// [a: 1]\r\n\r\nConsole.WriteLine();}", "M(1);")]
        [InlineData("void M(in int a) {} // [a: 1]", "M(1);")]
        [InlineData("void M(ref int a) {} // [a: 1]", "int x = 1; M(ref x); // [x: 1]\r\n")]
        [InlineData("void M(int a, int b) {} // [a: 1, b: 2]", "M(1, 2);")]
        [InlineData("void M(int a, out int b) { b = 1; } // [a: 1]", "M(1, out var _);")]
        [InlineData("void M(int a, int b = 0) {} // [a: 1, b: 0]", "M(1);")]
        public async Task Flow_IncludesExpectedValues_ForCSharpStaticMethodArguments(
            string methodCode, string methodCallCode/*,
            bool expectedSkipped = false*/
        ) {
            // Arrange
            var code = @"
                using System;
                public static class Program {
                    public static void Main() { " + methodCallCode + @" }
                    static " + methodCode + @"
                }
            ";

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            // Assert
            TestOutput.AssertFlowMatchesComments(code, output, _testOutputHelper);
        }

        [Fact]
        public async Task Flow_IncludesExpectedValues_ForCSharpInstanceMethodArguments() {
            // Arrange
            var code = @"
                using System;
                public class Program {
                    public static void Main() { new Program().M(1); }
                    public void M(int a) {} // [a: 1]
                }
            ";

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            // Assert
            TestOutput.AssertFlowMatchesComments(code, output, _testOutputHelper);
        }

        [Fact]
        public async Task Flow_IncludesExpectedValues_ForCSharpConstructorArguments() {
            // Arrange
            var code = @"
                using System;
                public class Program {
                    Program(int a) {} // [a: 1]
                    public static void Main() { new Program(1); }
                }
            ";

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            // Assert
            TestOutput.AssertFlowMatchesComments(code, output, _testOutputHelper);
        }

        [Fact]
        public async Task Flow_IncludesReturnValueInOutput() {
            // Arrange
            var code = @"
                public static class Program {
                    public static int Main() { return 3; } // [return: 3]
                }
            ";

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            // Assert
            TestOutput.AssertFlowMatchesComments(code, output, _testOutputHelper);
        }
    }
}
