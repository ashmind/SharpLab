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
using Xunit;
using Xunit.Abstractions;
using SharpLab.Server;
using SharpLab.Server.Common;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests {
    public class DecompilationTests {
        private readonly ITestOutputHelper _output;

        public DecompilationTests(ITestOutputHelper output) {
            _output = output;
        }

        [Theory]
        [InlineData("class C { void M((int, string) t) {} }")] // Tuples, https://github.com/ashmind/SharpLab/issues/139
        public async Task SlowUpdate_DecompilesSimpleCodeWithoutErrors(string code) {
            var driver = MirrorSharpTestDriver.New(TestEnvironment.MirrorSharpOptions).SetText(code);
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, LanguageNames.CSharp);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.NotNull(result.ExtensionResult);
            Assert.NotEmpty(result.ExtensionResult);
        }

        [Theory]
        [InlineData("Constructor.BaseCall.cs2cs")]
        [InlineData("NullPropagation.ToTernary.cs2cs")]
        [InlineData("Simple.cs2il")]
        [InlineData("Simple.vb2cs")]
        [InlineData("Module.vb2cs")]
        [InlineData("Lambda.CallInArray.cs2cs")] // https://github.com/ashmind/SharpLab/issues/9
        [InlineData("Cast.ExplicitOperatorOnNull.cs2cs")] // https://github.com/ashmind/SharpLab/issues/20
        [InlineData("Goto.TryWhile.cs2cs")] // https://github.com/ashmind/SharpLab/issues/123
        [InlineData("Nullable.OperatorLifting.cs2cs")] // https://github.com/ashmind/SharpLab/issues/159
        [InlineData("Finalizer.Exception.cs2il")] // https://github.com/ashmind/SharpLab/issues/205
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode(string resourceName) {
            var data = TestData.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            _output.WriteLine(decompiledText ?? "<null>");
            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(data.Expected, decompiledText);
        }

        [Theory]
        [InlineData("Condition.SimpleSwitch.cs2cs")] // https://github.com/ashmind/SharpLab/issues/25
        //[InlineData("Variable.FromArgumentToCall.cs2cs")] // https://github.com/ashmind/SharpLab/issues/128
        [InlineData("Preprocessor.IfDebug.cs2cs")] // https://github.com/ashmind/SharpLab/issues/161
        [InlineData("Preprocessor.IfDebug.vb2cs")] // https://github.com/ashmind/SharpLab/issues/161
        [InlineData("FSharp.Preprocessor.IfDebug.fs2cs")] // https://github.com/ashmind/SharpLab/issues/161
        [InlineData("Using.Simple.cs2cs")] // https://github.com/ashmind/SharpLab/issues/185
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_InDebug(string resourceName) {
            var data = TestData.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data, Optimize.Debug);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            _output.WriteLine(decompiledText ?? "<null>");
            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(data.Expected, decompiledText);
        }

        [Theory]
        [InlineData(LanguageNames.CSharp,"/// <summary><see cref=\"Incorrect\"/></summary>\r\npublic class C {}", "CS1574")] // https://github.com/ashmind/SharpLab/issues/219
        [InlineData(LanguageNames.VisualBasic, "''' <summary><see cref=\"Incorrect\"/></summary>\r\nPublic Class C\r\nEnd Class", "BC42309")]
        public async Task SlowUpdate_ReturnsExpectedWarnings_ForXmlDocumentation(string sourceLanguageName, string code, string expectedWarningId) {
            var driver = MirrorSharpTestDriver.New(TestEnvironment.MirrorSharpOptions).SetText(code);
            await driver.SendSetOptionsAsync(sourceLanguageName, TargetNames.IL);

            var result = await driver.SendSlowUpdateAsync<string>();
            Assert.Equal(
                new[] { new { Severity = "warning", Id = expectedWarningId } },
                result.Diagnostics.Select(d => new { d.Severity, d.Id }).ToArray()
            );
        }

        [Theory]
        [InlineData(LanguageNames.CSharp, "public class C {}")]
        [InlineData(LanguageNames.VisualBasic, "Public Class C\r\nEnd Class")]
        public async Task SlowUpdate_DoesNotReturnWarnings_ForCodeWithoutXmlDocumentation(string sourceLanguageName, string code) {
            var driver = MirrorSharpTestDriver.New(TestEnvironment.MirrorSharpOptions).SetText(code);
            await driver.SendSetOptionsAsync(sourceLanguageName, TargetNames.IL);

            var result = await driver.SendSlowUpdateAsync<string>();
            Assert.Empty(result.Diagnostics);
        }

        [Theory]
        [InlineData("FSharp.EmptyType.fs2il")]
        [InlineData("FSharp.SimpleMethod.fs2cs")] // https://github.com/ashmind/SharpLab/issues/119
        [InlineData("FSharp.NotNull.fs2cs")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_ForFSharp(string resourceName) {
            var data = TestData.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            _output.WriteLine(decompiledText ?? "<null>");
            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(data.Expected, decompiledText);
        }

        [Theory]
        [InlineData("JitAsm.Simple.cs2asm")]
        [InlineData("JitAsm.MultipleReturns.cs2asm")]
        [InlineData("JitAsm.ArrayElement.cs2asm")]
        [InlineData("JitAsm.AsyncRegression.cs2asm")]
        [InlineData("JitAsm.ConsoleWrite.cs2asm")]
        [InlineData("JitAsm.JumpBack.cs2asm")] // https://github.com/ashmind/SharpLab/issues/229
        [InlineData("JitAsm.Delegate.cs2asm")]
        [InlineData("JitAsm.Nested.Simple.cs2asm")]
        [InlineData("JitAsm.Generic.Open.Multiple.cs2asm")]
        [InlineData("JitAsm.Generic.MethodWithAttribute.cs2asm")]
        [InlineData("JitAsm.Generic.ClassWithAttribute.cs2asm")]
        [InlineData("JitAsm.Generic.MethodWithAttribute.fs2asm")]
        [InlineData("JitAsm.Generic.Nested.AttributeOnTop.cs2asm")]
        [InlineData("JitAsm.Generic.Nested.AttributeOnNested.cs2asm")]
        [InlineData("JitAsm.Generic.Nested.AttributeOnBoth.cs2asm")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_ForJitAsm(string resourceName) {
            var data = TestData.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = MakeJitAsmComparable(result.ExtensionResult?.Trim());
            _output.WriteLine(decompiledText ?? "<null>");
            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(data.Expected, decompiledText);
        }

        [Theory]
        [InlineData("class C { static int F = 0; }")]
        [InlineData("class C { static C() {} }")]
        [InlineData("class C { class N { static N() {} } }")]
        public async Task SlowUpdate_ReturnsNotSupportedError_ForJitAsmWithStaticConstructors(string code) {
            var driver = MirrorSharpTestDriver.New(TestEnvironment.MirrorSharpOptions).SetText(code);
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.JitAsm);

            await Assert.ThrowsAsync<NotSupportedException>(() => driver.SendSlowUpdateAsync<string>());
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

        private static async Task<MirrorSharpTestDriver> NewTestDriverAsync(TestData data, string optimize = Optimize.Release) {
            var driver = MirrorSharpTestDriver.New(TestEnvironment.MirrorSharpOptions);
            await driver.SendSetOptionsAsync(data.SourceLanguageName, data.TargetLanguageName, optimize);
            driver.SetText(data.Original);
            return driver;
        }

        private class TestData {
            private static readonly IDictionary<string, string> LanguageMap = new Dictionary<string, string> {
                { "cs",  LanguageNames.CSharp },
                { "vb",  LanguageNames.VisualBasic },
                { "fs",  LanguageNames.FSharp },
                { "il",  TargetNames.IL },
                { "asm", TargetNames.JitAsm },
                { "ast", TargetNames.Ast },
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
