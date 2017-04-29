using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            data.AssertMatches(result.ExtensionResult.Decompiled.Trim());
        }

        [Theory]
        [InlineData("JitAsm.Simple.cs2asm")]
        [InlineData("JitAsm.MultipleReturns.cs2asm")]
        [InlineData("JitAsm.ArrayElement.cs2asm")]
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
            data.AssertMatches(result.ExtensionResult.Decompiled.Trim());
        }

        private class TestData {
            private static readonly IDictionary<string, string> LanguageMap = new Dictionary<string, string> {
                { "cs",  LanguageNames.CSharp },
                { "vb",  LanguageNames.VisualBasic },
                { "il",  "IL" },
                { "asm", "JIT ASM" },
            };
            public string SourceText { get; }
            public Action<string> AssertMatches { get; }
            public string SourceLanguageName { get; }
            public string TargetLanguageName { get; }

            public TestData(string sourceText, Action<string> assertMatches, string sourceLanguageName, string targetLanguageName) {
                SourceText = sourceText;
                AssertMatches = assertMatches;
                SourceLanguageName = sourceLanguageName;
                TargetLanguageName = targetLanguageName;
            }

            public static TestData FromResource(string name) {
                var content = EmbeddedResource.ReadAllText(typeof(DecompilationTests), "TestCode." + name);
                var parts = content.Split("#=>");
                var sourceText = parts[0].Trim();
                var expected = parts[1].Trim();
                // ReSharper disable once PossibleNullReferenceException
                var fromTo = Path.GetExtension(name).TrimStart('.').Split('2').Select(x => LanguageMap[x]).ToList();

                if (!expected.Contains("#"))
                    return new TestData(sourceText, a => Assert.Equal(expected, a), fromTo[0], fromTo[1]);

                var expectedPattern = ParseAsPattern(expected);
                return new TestData(sourceText, a => Assert.Matches(expectedPattern, a), fromTo[0], fromTo[1]);
            }

            private static string ParseAsPattern(string expected) {
                return "^" + Regex.Replace(expected, "#/(.+)/#|([^#]+)", m => {
                    var patternGroup = m.Groups[1];
                    if (patternGroup.Success)
                        return patternGroup.Value;
                    return Regex.Escape(m.Groups[2].Value)
                        .Replace(@"\ ", " ").Replace("\\r", "\r").Replace("\\n", "\n");
                }) + "$";
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ExtensionResult {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Decompiled { get; set; }
        }
    }
}
