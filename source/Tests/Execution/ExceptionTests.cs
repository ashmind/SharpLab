using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class ExceptionTests {
        private readonly ITestOutputHelper _outputHelper;

        public ExceptionTests(ITestOutputHelper outputHelper) {
            _outputHelper = outputHelper;
            // TestDiagnosticLog.Enable(outputHelper);
        }

        [Theory]
        [InlineData("DivideByZero.cs")]
        [InlineData("DivideByZero.Catch.cs")]
        [InlineData("DivideByZero.Catch.When.True.cs")]
        [InlineData("DivideByZero.Catch.When.False.cs")]
        [InlineData("DivideByZero.Finally.cs")]
        [InlineData("DivideByZero.Catch.Finally.cs")]
        [InlineData("DivideByZero.Catch.Finally.WriteLine.cs", OptimizationLevel.Debug)]
        [InlineData("DivideByZero.Catch.Finally.WriteLine.Release.cs", OptimizationLevel.Release)]
        [InlineData("Throw.New.Finally.cs", OptimizationLevel.Debug)]
        public async Task Exceptions_AreReportedInFlow(
            string codeFileName, OptimizationLevel optimizationLevel = OptimizationLevel.Debug
        ) {
            // Arrange
            var code = await TestCode.FromCodeOnlyFileAsync("Flow/Exceptions/" + codeFileName);

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code, optimizationLevel: optimizationLevel);

            // Assert
            TestOutput.AssertFlowMatchesValueComments(code, output, _outputHelper);
        }

        [Fact]
        public async Task Exceptions_AreIncludedInOutput() {
            // Arrange
            var code = @"
                public static class Program {
                    public static int Main() { throw new System.Exception(""Test""); }
                }
            ";

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            // Assert
            Assert.StartsWith(
                @"#{""type"":""inspection:simple"",""title"":""Exception"",""value"":""System.Exception: Test",
                output
            );
        }
    }
}
