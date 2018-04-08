using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MirrorSharp.Testing;
using Xunit;
using SharpLab.Server.Common;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests {
    public class ExplanationTests {
        [Fact]
        public async Task SlowUpdate_ExplainsExpressionBodiedProperties() {
            var driver = await NewTestDriverAsync();
            driver.SetText("class C { int P => 1; }");

            var result = await driver.SendSlowUpdateAsync<ExplanationData[]>();

            var explanation = Assert.Single(result.ExtensionResult);
            // space at the end is expected -- currently extra spaces are trimmed by JS
            Assert.Equal("int P => 1; ", explanation.Code);
            Assert.Equal("expression-bodied member", explanation.Name);
            Assert.NotEmpty(explanation.Text);
            Assert.Equal("https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-6#expression-bodied-function-members", explanation.Link);
        }

        private static async Task<MirrorSharpTestDriver> NewTestDriverAsync() {
            var driver = MirrorSharpTestDriver.New(TestEnvironment.MirrorSharpOptions);
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.Explain);
            return driver;
        }

        private class ExplanationData {
            public string Code { get; set; }
            public string Name { get; set; }
            public string Text { get; set; }
            public string Link { get; set; }
        }
    }
}
