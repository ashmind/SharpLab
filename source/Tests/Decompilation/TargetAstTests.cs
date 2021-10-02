using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Decompilation {
    public class TargetAstTests {
        private readonly ITestOutputHelper _output;

        public TargetAstTests(ITestOutputHelper output) {
            _output = output;
            // TestAssemblyLog.Enable(output);
        }

        [Theory]
        [InlineData("Ast.EmptyClass.cs2ast")]
        [InlineData("Ast.StructuredTrivia.cs2ast")]
        [InlineData("Ast.LiteralTokens.cs2ast")]
        [InlineData("Ast.EmptyType.fs")]
        [InlineData("Ast.LiteralTokens.fs")]
        public async Task SlowUpdate_ReturnsExpectedResult(string resourceName) {
            var code = TestCode.FromResource(resourceName);
            var driver = await TestDriverFactory.FromCodeAsync(code);

            var result = await driver.SendSlowUpdateAsync<JArray>();

            var json = result.ExtensionResult?.ToString();

            code.AssertIsExpected(json, _output);
        }
    }
}
