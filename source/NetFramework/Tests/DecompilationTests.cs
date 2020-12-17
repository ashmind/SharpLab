using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MirrorSharp.Advanced.EarlyAccess;
using MirrorSharp.Testing;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
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
            var driver = TestEnvironment.NewDriver().SetText(code);
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, LanguageNames.CSharp);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            Assert.True(string.IsNullOrEmpty(errors), errors);
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
        [InlineData("Parameters.Optional.Decimal.cs2cs")] // https://github.com/ashmind/SharpLab/issues/316
        [InlineData("Unsafe.FixedBuffer.cs2cs")] // https://github.com/ashmind/SharpLab/issues/398
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode(string resourceName) {
            var data = TestCode.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            Assert.True(string.IsNullOrEmpty(errors), errors);
            data.AssertIsExpected(decompiledText, _output);
        }

        [Theory]
        [InlineData("Condition.SimpleSwitch.cs2cs")] // https://github.com/ashmind/SharpLab/issues/25
        //[InlineData("Variable.FromArgumentToCall.cs2cs")] // https://github.com/ashmind/SharpLab/issues/128
        [InlineData("Preprocessor.IfDebug.cs2cs")] // https://github.com/ashmind/SharpLab/issues/161
        [InlineData("Preprocessor.IfDebug.vb2cs")] // https://github.com/ashmind/SharpLab/issues/161
        [InlineData("FSharp.Preprocessor.IfDebug.fs2cs")] // https://github.com/ashmind/SharpLab/issues/161
        [InlineData("Using.Simple.cs2cs")] // https://github.com/ashmind/SharpLab/issues/185
        [InlineData("StringInterpolation.Simple.cs2cs")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_InDebug(string resourceName) {
            var data = TestCode.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data, Optimize.Debug);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            Assert.True(string.IsNullOrEmpty(errors), errors);
            data.AssertIsExpected(decompiledText, _output);
        }

        [Theory]
        [InlineData(LanguageNames.CSharp,"/// <summary><see cref=\"Incorrect\"/></summary>\r\npublic class C {}", "CS1574")] // https://github.com/ashmind/SharpLab/issues/219
        [InlineData(LanguageNames.VisualBasic, "''' <summary><see cref=\"Incorrect\"/></summary>\r\nPublic Class C\r\nEnd Class", "BC42309")]
        public async Task SlowUpdate_ReturnsExpectedWarnings_ForXmlDocumentation(string sourceLanguageName, string code, string expectedWarningId) {
            var driver = TestEnvironment.NewDriver().SetText(code);
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
            var driver = TestEnvironment.NewDriver().SetText(code);
            await driver.SendSetOptionsAsync(sourceLanguageName, TargetNames.IL);

            var result = await driver.SendSlowUpdateAsync<string>();
            Assert.Empty(result.Diagnostics);
        }

        [Theory]
        [InlineData("FSharp.EmptyType.fs2il")]
        [InlineData("FSharp.SimpleMethod.fs2cs")] // https://github.com/ashmind/SharpLab/issues/119
        [InlineData("FSharp.NotNull.fs2cs")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_ForFSharp(string resourceName) {
            var data = TestCode.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            Assert.True(string.IsNullOrEmpty(errors), errors);
            data.AssertIsExpected(decompiledText, _output);
        }

        [Theory]
        [InlineData("JitAsm.Simple.cs2asm")]
        [InlineData("JitAsm.MultipleReturns.cs2asm")]
        [InlineData("JitAsm.ArrayElement.cs2asm")]
        #if APPVEYOR
        // not quite sure why some ASM symbols are not resolved when running in AppVeyor
        [InlineData("JitAsm.AsyncRegression.AppVeyor.cs2asm")]
        #else
        [InlineData("JitAsm.AsyncRegression.cs2asm")]
        #endif
        [InlineData("JitAsm.ConsoleWrite.cs2asm")]
        [InlineData("JitAsm.JumpBack.cs2asm")] // https://github.com/ashmind/SharpLab/issues/229
        [InlineData("JitAsm.Delegate.cs2asm")]
        [InlineData("JitAsm.Nested.Simple.cs2asm")]
        [InlineData("JitAsm.Generic.Open.Multiple.cs2asm")]
        [InlineData("JitAsm.Generic.MethodWithAttribute.cs2asm")]
        [InlineData("JitAsm.Generic.ClassWithAttribute.cs2asm")]
        #if !NETCOREAPP
        // TODO: Diagnose later
        // [InlineData("JitAsm.Generic.MethodWithAttribute.fs2asm")]
        #endif
        [InlineData("JitAsm.Generic.Nested.AttributeOnTop.cs2asm")]
        [InlineData("JitAsm.Generic.Nested.AttributeOnNested.cs2asm")]
        [InlineData("JitAsm.Generic.Nested.AttributeOnBoth.cs2asm")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_ForJitAsm(string resourceName) {
            var data = TestCode.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            Assert.True(string.IsNullOrEmpty(errors), errors);
            data.AssertIsExpected(decompiledText, _output);
        }      

        [Theory]
        [InlineData("class C { static int F = 0; }")]
        [InlineData("class C { static C() {} }")]
        [InlineData("class C { class N { static N() {} } }")]
        public async Task SlowUpdate_ReturnsNotSupportedError_ForJitAsmWithStaticConstructors(string code) {
            var driver = TestEnvironment.NewDriver().SetText(code);
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
            var data = TestCode.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<JArray>();

            var json = result.ExtensionResult?.ToString();

            data.AssertIsExpected(json, _output);
        }

        [Theory]
        [InlineData("class X<A,B,C,D,E>{class Y:X<Y,Y,Y,Y,Y>{Y.Y.Y.Y.Y.Y.Y.Y.Y y;}}")] // https://codegolf.stackexchange.com/a/69200
        public async Task SlowUpdate_ReturnsRoslynGuardException_ForCompilerBombs(string code) {
            var driver = TestEnvironment.NewDriver().SetText(code);
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.IL);

            await Assert.ThrowsAsync<RoslynGuardException>(() => driver.SendSlowUpdateAsync<string>());
        }

        private static async Task<MirrorSharpTestDriver> NewTestDriverAsync(TestCode code, string optimize = Optimize.Release) {
            var driver = TestEnvironment.NewDriver();
            await driver.SendSetOptionsAsync(code.SourceLanguageName, code.TargetName, optimize);
            driver.SetText(code.Original);
            return driver;
        }
    }
}
