using System;
using System.Linq;
using System.Threading.Tasks;
using MirrorSharp.Advanced.EarlyAccess;
using MirrorSharp.Testing;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using SharpLab.Server.Common;
using SharpLab.Tests.Internal;
using System.Runtime.Intrinsics.X86;
using SharpLab.Runtime.Internal;

namespace SharpLab.Tests {
    public class DecompilationTests {
        private readonly ITestOutputHelper _output;

        public DecompilationTests(ITestOutputHelper output) {
            _output = output;
            // TestAssemblyLog.Enable(output);
        }

        [Theory]
        [InlineData("class C { void M((int, string) t) {} }")] // Tuples, https://github.com/ashmind/SharpLab/issues/139
        public async Task SlowUpdate_DecompilesSimpleCodeWithoutErrors(string code) {
            var driver = TestEnvironment.NewDriver().SetText(code);
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.CSharp);

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
        [InlineData("Switch.String.Large.cs2cs")]
        [InlineData("Lock.Simple.cs2cs")]
        [InlineData("Property.InitOnly.cs2cs")]
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
        [InlineData("StringInterpolation.Simple.cs")]
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
        [InlineData("FSharp.EmptyType.fs")]
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
        [InlineData("IL.EmptyMethod.il")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_ForIL(string resourceName) {
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
        [InlineData("JitAsm.AsyncRegression.cs2asm")]
        [InlineData("JitAsm.ConsoleWrite.cs2asm")]
        [InlineData("JitAsm.JumpBack.cs2asm")] // https://github.com/ashmind/SharpLab/issues/229
        [InlineData("JitAsm.Delegate.cs2asm")]
        [InlineData("JitAsm.Nested.Simple.cs2asm")]
        [InlineData("JitAsm.Generic.Open.Multiple.cs2asm")]
        [InlineData("JitAsm.Generic.MethodWithAttribute.cs2asm")]
        [InlineData("JitAsm.Generic.ClassWithAttribute.cs")]
        #if !NETCOREAPP
        // TODO: Diagnose later
        // [InlineData("JitAsm.Generic.MethodWithAttribute.fs2asm")]
        #endif
        [InlineData("JitAsm.Generic.Nested.AttributeOnTop.cs")]
        [InlineData("JitAsm.Generic.Nested.AttributeOnNested.cs")]
        [InlineData("JitAsm.Generic.Nested.AttributeOnBoth.cs")]
        [InlineData("JitAsm.Vectors.Avx2.cs2asm")]
        [InlineData("JitAsm.Math.FusedMultiplyAdd.Fma.cs2asm")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_ForJitAsm(string resourceName) {
            // https://github.com/ashmind/SharpLab/issues/514
            if (resourceName.Contains(".Fma.") && !Fma.IsSupported)
                resourceName = resourceName.Replace(".Fma.", ".NoFma.");
            if (resourceName.Contains(".Avx2.") && !Avx2.IsSupported)
                resourceName = resourceName.Replace(".Avx2.", ".NoAvx2.");

            var data = TestCode.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            Assert.True(string.IsNullOrEmpty(errors), errors);
            data.AssertIsExpected(decompiledText, _output);
        }

        [Theory]
        [InlineData("class C { static int F = 1; }")]
        [InlineData("class C { static C() {} }")]
        [InlineData("class C { class N { static N() {} } }")]
        public async Task SlowUpdate_ReturnsNotSupportedError_ForJitAsmWithStaticConstructors(string code) {
            var driver = TestEnvironment.NewDriver().SetText(code);
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.JitAsm);

            await Assert.ThrowsAsync<NotSupportedException>(() => driver.SendSlowUpdateAsync<string>());
        }

        [Theory]
        [InlineData("[JitGeneric(typeof(int), typeof(int))] class C<T> {}")]
        [InlineData("[JitGeneric(typeof(Span<int>))] class C<T> {}")]
        [InlineData("class C { [JitGeneric(typeof(int), typeof(int))] void M<T>() {} }")]
        [InlineData("class C { [JitGeneric(typeof(Span<int>))] void M<T>() {} }")]
        public async Task SlowUpdate_ReturnsJitGenericAttributeException_ForIncorrectJitGenericArguments(string code) {
            var driver = TestEnvironment.NewDriver().SetText($@"
                using System;
                using SharpLab.Runtime;
                {code}
            ");
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.JitAsm);

            var exception = await Record.ExceptionAsync(() => driver.SendSlowUpdateAsync<string>());

            Assert.IsType<JitGenericAttributeException>(exception);
        }

        [Theory]
        [InlineData("Ast.EmptyClass.cs2ast")]
        [InlineData("Ast.StructuredTrivia.cs2ast")]
        [InlineData("Ast.LiteralTokens.cs2ast")]
        [InlineData("Ast.EmptyType.fs")]
        [InlineData("Ast.LiteralTokens.fs")]
        public async Task SlowUpdate_ReturnsExpectedResult_ForAst(string resourceName) {
            var data = TestCode.FromResource(resourceName);
            var driver = await NewTestDriverAsync(data);

            var result = await driver.SendSlowUpdateAsync<JArray>();

            var json = result.ExtensionResult?.ToString();

            data.AssertIsExpected(json, _output);
        }

        [Theory]
        [InlineData(LanguageNames.CSharp, "class X<A,B,C,D,E> { class Y: X<Y,Y,Y,Y,Y> {Y.Y.Y.Y.Y.Y.Y.Y.Y y; } }")] // https://codegolf.stackexchange.com/a/69200
        [InlineData(LanguageNames.VisualBasic, @"
            Class X (Of A, B, C, D, E)
                Class Y Inherits X (Of Y, Y, Y, Y, Y)
                    Private y As Y.Y.Y.Y.Y.Y.Y.Y.Y
                End Class
            End Class
        ")]
        public async Task SlowUpdate_ReturnsRoslynGuardException_ForCompilerBombs(string languageName, string code) {
            var driver = TestEnvironment.NewDriver().SetText(code);
            await driver.SendSetOptionsAsync(languageName, TargetNames.IL);

            await Assert.ThrowsAsync<RoslynCompilationGuardException>(() => driver.SendSlowUpdateAsync<string>());
        }

        [Theory]
        [InlineData("x[][][][][]")]
        [InlineData("x [,,,] [,] [,,,]   [,,,] [,]")]
        [InlineData("x [] [] []   [] []")]
        [InlineData("x[1][2][3][4][5]")]
        [InlineData("x[[[[[][][][][]]]]]")]
        [InlineData("x[[[[[[]]]]]]")]
        [InlineData("x()()()()()")]
        [InlineData("x (,,,) (,) (,,,)   (,,,) (,)")]
        [InlineData("x () () ()   () ()")]
        [InlineData("x(1)(2)(3)(4)(5)")]
        [InlineData("x((((()()()()()))))")]
        [InlineData("x(((((())))))")]
        public async Task SetOptions_ReturnsRoslynGuardException_ForTextExceedingTokenLimits(string code) {
            var driver = TestEnvironment.NewDriver().SetText(code);

            await Assert.ThrowsAsync<RoslynSourceTextGuardException>(() => driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.IL));
        }

        [Theory]
        [InlineData("Append(Append(Append(Append(hash, (byte)value), value>>8), value>>16), value>>24)")]
        public async Task SetOptions_ProcessesTokenEdgeCases_WithoutTokenValidationErrors(string code) {
            var driver = TestEnvironment.NewDriver().SetText(code);

            var exception = await Record.ExceptionAsync(() => driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.IL));

            Assert.Null(exception);
        }

        private static async Task<MirrorSharpTestDriver> NewTestDriverAsync(TestCode code, string optimize = Optimize.Release) {
            var driver = TestEnvironment.NewDriver();
            await driver.SendSetOptionsAsync(code.SourceLanguageName, code.TargetName, optimize);
            driver.SetText(code.Original);
            return driver;
        }
    }
}
