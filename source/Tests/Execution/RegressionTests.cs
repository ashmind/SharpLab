using System.Threading.Tasks;
using SharpLab.Server.Common;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;
using Xunit;

namespace SharpLab.Tests.Execution {
    public class RegressionTests {
        [Theory]
        [InlineData("CertainLoop.cs")]
        [InlineData("FSharpNestedLambda.fs", LanguageNames.FSharp)]
        [InlineData("NestedAnonymousObject.cs")]
        [InlineData("ReturnRef.cs")]
        [InlineData("CatchWithNameSameLineAsClosingTryBracket.cs")]
        [InlineData("MoreThanFourArguments.cs")]
        [InlineData("InitOnlyProperty.cs")]
        [InlineData("TopLevelLocalConstant.cs")]
        public async Task Execution_DoesNotFail(string codeFileName, string languageName = LanguageNames.CSharp) {
            // Arrange
            var code = await TestCode.FromCodeOnlyFileAsync("Regression/" + codeFileName);

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code, languageName);

            // Assert
            Assert.DoesNotMatch("Exception:", output);
        }
    }
}
