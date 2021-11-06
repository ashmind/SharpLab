using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class VisualBasicTests {
        [Fact]
        public async Task VisualBasic_Simple() {
            // Arrange
            var code = @"
                Imports System
                Public Module Program
                    Public Sub Main()
                        Console.Write(""Test"")
                    End Sub
                End Module
            ";

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code, LanguageNames.VisualBasic);

            // Assert
            Assert.Equal("Test", TestOutput.RemoveFlowJson(output));
        }
    }
}
