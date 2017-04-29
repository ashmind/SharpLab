using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AshMind.Extensions;
using Microsoft.CodeAnalysis;
using MirrorSharp.Testing;
using Pedantic.IO;
using TryRoslyn.Server;
using Xunit;
using Xunit.Abstractions;

namespace TryRoslyn.Tests {
    public class DecompilationTests {
        private readonly ITestOutputHelper _output;

        public DecompilationTests(ITestOutputHelper output) {
            _output = output;
        }

        [Theory]
        [InlineData("Constructor.BaseCall.cs2cs")]
        [InlineData("NullPropagation.ToTernary.cs2cs")]
        [InlineData("Simple.cs2il")]
        [InlineData("Simple.vb2vb")]
        [InlineData("Module.vb2vb")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode(string resourceName) {
            var data = TestData.FromResource(resourceName);

            var driver = MirrorSharpTestDriver.New(Startup.CreateMirrorSharpOptions());
            await driver.SendSetOptionsAsync(new Dictionary<string, string> {
                { "language", data.SourceLanguageName },
                { "optimize", nameof(OptimizationLevel.Release).ToLowerInvariant() },
                { "x-target-language", data.TargetLanguageName }
            });
            driver.SetSourceText(data.SourceText);

            var result = await driver.SendSlowUpdateAsync<ExtensionResult>();
            var errors = result.JoinErrors();

            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(data.Expected, result.ExtensionResult.Decompiled.Trim());
        }

        [Theory]
        [InlineData("JitAsm.Simple.cs2asm")]
        [InlineData("JitAsm.MultipleReturns.cs2asm")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_ForJitAsm(string resourceName) {
            var data = TestData.FromResource(resourceName);
            var driver = MirrorSharpTestDriver.New(Startup.CreateMirrorSharpOptions());
            await driver.SendSetOptionsAsync(new Dictionary<string, string> {
                { "language", data.SourceLanguageName },
                { "optimize", nameof(OptimizationLevel.Release).ToLowerInvariant() },
                { "x-target-language", data.TargetLanguageName }
            });
            driver.SetSourceText(data.SourceText);

            var result = await driver.SendSlowUpdateAsync<ExtensionResult>();
            var errors = result.JoinErrors();

            _output.WriteLine(result.ExtensionResult.Decompiled.Trim());

            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(data.Expected, result.ExtensionResult.Decompiled.Trim());
        }

        private class TestData {
            private static readonly IDictionary<string, string> LanguageMap = new Dictionary<string, string> {
                { "cs",  LanguageNames.CSharp },
                { "vb",  LanguageNames.VisualBasic },
                { "il",  "IL" },
                { "asm", "JIT ASM" },
            };
            public string SourceText { get; }
            public string Expected { get; }
            public string SourceLanguageName { get; }
            public string TargetLanguageName { get; }

            public TestData(string sourceText, string expected, string sourceLanguageName, string targetLanguageName) {
                SourceText = sourceText;
                Expected = expected;
                SourceLanguageName = sourceLanguageName;
                TargetLanguageName = targetLanguageName;
            }

            public static TestData FromResource(string name) {
                var content = EmbeddedResource.ReadAllText(typeof(DecompilationTests), "TestCode." + name);
                var parts = content.Split("?=>");
                var sourceText = parts[0].Trim();
                var expected = parts[1].Trim();
                // ReSharper disable once PossibleNullReferenceException
                var fromTo = Path.GetExtension(name).TrimStart('.').Split('2');

                return new TestData(sourceText, expected, LanguageMap[fromTo[0]], LanguageMap[fromTo[1]]);
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ExtensionResult {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Decompiled { get; set; }
        }
    }
}
