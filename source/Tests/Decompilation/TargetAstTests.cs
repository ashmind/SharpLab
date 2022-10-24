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
            // TestDiagnosticLog.Enable(output);
        }

        [Theory]
        [InlineData("Ast/EmptyClass.cs2ast")]
        [InlineData("Ast/StructuredTrivia.cs2ast")]
        [InlineData("Ast/LiteralTokens.cs2ast")]
        [InlineData("Ast/EmptyType.fs")]
        [InlineData("Ast/LiteralTokens.fs")]
        public async Task SlowUpdate_ReturnsExpectedResult(string codeFilePath) {
            var code = await TestCode.FromFileAsync(codeFilePath);
            var driver = await TestDriverFactory.FromCodeAsync(code);

            var result = await driver.SendSlowUpdateAsync<JArray>();

            var json = result.ExtensionResult?.ToString();

            await code.AssertIsExpectedAsync(json, _output);
        }
    }
}
