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
using Newtonsoft.Json.Linq;
using Pedantic.IO;
using SharpLab.Server;
using Xunit;
using Xunit.Abstractions;

namespace SharpLab.Tests {
    public class DecompilationTests {
        private static readonly MirrorSharpOptions MirrorSharpOptions = Startup.CreateMirrorSharpOptions();
        private readonly ITestOutputHelper _output;

        public DecompilationTests(ITestOutputHelper output) {
            _output = output;
        }

        [Theory]
        // Tuples, https://github.com/ashmind/SharpLab/issues/139
        [InlineData("class C { void M((int, string) t) {} }")]
        public async Task SlowUpdate_DecompilesSimpleCodeWithoutErrors(string code) {
            var driver = MirrorSharpTestDriver.New().SetText(code);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            Assert.True(errors.IsNullOrEmpty(), errors);
        }

        [Theory]
        [InlineData("Constructor.BaseCall.cs2cs")]
        [InlineData("NullPropagation.ToTernary.cs2cs")]
        [InlineData("Simple.cs2il")]
        [InlineData("Simple.vb2vb")]
        [InlineData("Module.vb2vb")]
        // https://github.com/ashmind/SharpLab/issues/9
        [InlineData("Lambda.CallInArray.cs2cs")]
        // https://github.com/ashmind/SharpLab/issues/20
        [InlineData("Cast.ExplicitOperatorOnNull.cs2cs")]
        // https://github.com/ashmind/SharpLab/issues/123
        [InlineData("Goto.TryWhile.cs2cs")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode(string resourceName) {
            var data = TestData.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            _output.WriteLine(decompiledText);
            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(data.Expected, decompiledText);
        }

        [Theory]
        // https://github.com/ashmind/SharpLab/issues/25
        [InlineData("Condition.SimpleSwitch.cs2cs")]
        // https://github.com/ashmind/SharpLab/issues/128
        [InlineData("Variable.FromArgumentToCall.cs2cs")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_InDebug(string resourceName) {
            var data = TestData.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data, OptimizationLevel.Debug);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            _output.WriteLine(decompiledText);
            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(data.Expected, decompiledText);
        }

        [Theory]
        [InlineData("FSharp.EmptyType.fs2il")]
        [InlineData("FSharp.SimpleMethod.fs2cs")]
        // https://github.com/ashmind/SharpLab/issues/119
        [InlineData("FSharp.NotNull.fs2cs")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_ForFSharp(string resourceName) {
            var data = TestData.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
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
        [InlineData("JitAsm.OpenGenerics.cs2asm")]
        [InlineData("JitAsm.GenericMethodWithAttribute.cs2asm")]
        [InlineData("JitAsm.GenericClassWithAttribute.cs2asm")]
        [InlineData("JitAsm.GenericMethodWithAttribute.fs2asm")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_ForJitAsm(string resourceName) {
            var data = TestData.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = MakeJitAsmComparable(result.ExtensionResult?.Trim());
            _output.WriteLine(decompiledText ?? "");
            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(data.Expected, decompiledText);
        }

        [Theory]
        [InlineData("Ast.EmptyClass.cs2ast")]
        [InlineData("Ast.StructuredTrivia.cs2ast")]
        [InlineData("Ast.LiteralTokens.cs2ast")]
        [InlineData("Ast.EmptyType.fs2ast")]
        [InlineData("Ast.LiteralTokens.fs2ast")]
        public async Task SlowUpdate_ReturnsExpectedResult_ForAst(string resourceName) {
            var data = TestData.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<JArray>();

            var json = result.ExtensionResult?.ToString();

            _output.WriteLine(json ?? "<null>");
            Assert.Equal(data.Expected, json);
        }

        private static string MakeJitAsmComparable(string jitAsm) {
            if (jitAsm == null)
                return null;

            return Regex.Replace(
                jitAsm,
                @"((?<=0x)[\da-f]{7,8}(?=$|[^\da-f])|(?<=CLR v)[\d\.]+)", "<IGNORE>"
            );
        }

        private static async Task<MirrorSharpTestDriver> NewTestDriverAsync(TestData data, OptimizationLevel optimizationLevel = OptimizationLevel.Release) {
            var driver = MirrorSharpTestDriver.New(MirrorSharpOptions);
            await driver.SendSetOptionsAsync(new Dictionary<string, string> {
                {"language", data.SourceLanguageName},
                {"optimize", optimizationLevel.ToString().ToLowerInvariant()},
                {"x-target", data.TargetLanguageName}
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
                { "ast", "AST" },
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
    }
}
