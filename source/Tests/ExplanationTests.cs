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
            // some spaces are expected -- currently extra spaces are trimmed by JS
            Assert.Equal("int P => 1;", explanation.Code.Trim());
            Assert.Equal("expression-bodied member", explanation.Name);
            Assert.NotEmpty(explanation.Text);
            Assert.Equal("https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-6#expression-bodied-function-members", explanation.Link);
        }

        [Theory]
        [InlineData("class C { async void A() { var x = 5; } }", "async void A() { … }")]
        [InlineData("class C { async void A() {} }", "async void A() {}")]
        public async Task SlowUpdate_DoesNotIncludeBlockContentsInExplanationCode(string source, string expected) {
            var driver = await NewTestDriverAsync();
            driver.SetText(source);

            var result = await driver.SendSlowUpdateAsync<ExplanationData[]>();

            var explanation = Assert.Single(result.ExtensionResult);
            // some spaces are expected -- currently extra spaces are trimmed by JS
            Assert.Equal(expected, explanation.Code.Trim());
        }

        [Theory]
        [InlineData("class C { async void A(int a) {} }", "async void A(…) {}")]
        [InlineData("class C { async void A() {} }", "async void A() {}")]
        public async Task SlowUpdate_DoesNotIncludeParameterListsInExplanationCode(string source, string expected) {
            var driver = await NewTestDriverAsync();
            driver.SetText(source);

            var result = await driver.SendSlowUpdateAsync<ExplanationData[]>();

            var explanation = Assert.Single(result.ExtensionResult);
            // some spaces are expected -- currently extra spaces are trimmed by JS
            Assert.Equal(expected, explanation.Code.Trim());
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
