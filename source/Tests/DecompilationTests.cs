using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AshMind.Extensions;
using Microsoft.CodeAnalysis;
using MirrorSharp;
using MirrorSharp.Testing;
using Pedantic.IO;
using TryRoslyn.Server;
using Xunit;
using Xunit.Abstractions;

namespace TryRoslyn.Tests {
    public class DecompilationTests {
        private static readonly MirrorSharpOptions MirrorSharpOptions = Startup.CreateMirrorSharpOptions();
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
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<ExtensionResult>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult.Decompiled.Trim();
            _output.WriteLine(decompiledText);
            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(data.Expected, decompiledText);
        }

        [Theory]
        [InlineData("FSharp.EmptyType.fs2il")]
        [InlineData("FSharp.SimpleMethod.fs2cs")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_ForFSharp(string resourceName) {
            var data = TestData.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<ExtensionResult>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult.Decompiled.Trim();
            _output.WriteLine(decompiledText);
            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(data.Expected, decompiledText);
        }

        [Theory]
        [InlineData("JitAsm.Simple.cs2asm")]
        [InlineData("JitAsm.MultipleReturns.cs2asm")]
        [InlineData("JitAsm.ArrayElement.cs2asm")]
        [InlineData("JitAsm.AsyncRegression.cs2asm")]
        [InlineData("JitAsm.ConsoleWrite.cs2asm")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_ForJitAsm(string resourceName) {
            var data = TestData.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<ExtensionResult>();
            var errors = result.JoinErrors();

            var decompiledText = Regex.Replace(
                result.ExtensionResult.Decompiled.Trim(),
                @"((?<=0x)[\da-f]{7,8}(?=$|[^\da-f])|(?<=CLR v)[\d\.]+)", "<IGNORE>"
            );
            _output.WriteLine(decompiledText);
            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(data.Expected, decompiledText);
        }
        
        private static async Task<MirrorSharpTestDriver> NewTestDriverAsync(TestData data) {
            var driver = MirrorSharpTestDriver.New(MirrorSharpOptions);
            await driver.SendSetOptionsAsync(new Dictionary<string, string> {
                {"language", data.SourceLanguageName},
                {"optimize", nameof(OptimizationLevel.Release).ToLowerInvariant()},
                {"x-target-language", data.TargetLanguageName}
            });
            driver.SetText(data.Original);
            return driver;
        }

        private class TestData {
            private static readonly IDictionary<string, string> LanguageMap = new Dictionary<string, string> {
                { "cs",  LanguageNames.CSharp },
                { "vb",  LanguageNames.VisualBasic },
                { "fs",  "F#" },
                { "il",  "IL" },
                { "asm", "JIT ASM" },
            };
            public string Original { get; }
            public string Expected { get; }
            public string SourceLanguageName { get; }
            public string TargetLanguageName { get; }

            public TestData(string original, string expected, string sourceLanguageName, string targetLanguageName) {
                Original = original;
                Expected = expected;
                SourceLanguageName = sourceLanguageName;
                TargetLanguageName = targetLanguageName;
            }

            public static TestData FromResource(string name) {
                var content = EmbeddedResource.ReadAllText(typeof(DecompilationTests), "TestCode." + name);
                var parts = content.Split("#=>");
                var code = parts[0].Trim();
                var expected = parts[1].Trim();
                // ReSharper disable once PossibleNullReferenceException
                var fromTo = Path.GetExtension(name).TrimStart('.').Split('2').Select(x => LanguageMap[x]).ToList();
                
                return new TestData(code, expected, fromTo[0], fromTo[1]);
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ExtensionResult {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Decompiled { get; set; }
        }
    }
}
