using System.Linq;
using System.Threading.Tasks;
using MirrorSharp.Advanced.EarlyAccess;
using SharpLab.Server.Common;
using SharpLab.Tests.Internal;
using Xunit;
using Xunit.Abstractions;

namespace SharpLab.Tests.Decompilation {
    public class GeneralTests {
        private readonly ITestOutputHelper _output;

        public GeneralTests(ITestOutputHelper output) {
            _output = output;
            // TestDiagnosticLog.Enable(output);
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
        [InlineData("Constructor.BaseCall.cs")]
        [InlineData("NullPropagation.ToTernary.cs")]
        [InlineData("Simple.cs")]
        [InlineData("Simple.vb")]
        [InlineData("Module.vb")]
        [InlineData("Lambda.CallInArray.cs")] // https://github.com/ashmind/SharpLab/issues/9
        [InlineData("Cast.ExplicitOperatorOnNull.cs")] // https://github.com/ashmind/SharpLab/issues/20
        [InlineData("Goto.TryWhile.cs")] // https://github.com/ashmind/SharpLab/issues/123
        [InlineData("Nullable.OperatorLifting.cs")] // https://github.com/ashmind/SharpLab/issues/159
        [InlineData("Finalizer.Exception.cs")] // https://github.com/ashmind/SharpLab/issues/205
        [InlineData("Parameters.Optional.Decimal.cs")] // https://github.com/ashmind/SharpLab/issues/316
        [InlineData("Unsafe.FixedBuffer.cs")] // https://github.com/ashmind/SharpLab/issues/398
        [InlineData("Switch.String.Large.cs")]
        [InlineData("Lock.Simple.cs")]
        [InlineData("Property.InitOnly.cs")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode(string codeFilePath) {
            var code = await TestCode.FromFileAsync(codeFilePath);
            var driver = await TestDriverFactory.FromCodeAsync(code);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            Assert.True(string.IsNullOrEmpty(errors), errors);
            await code.AssertIsExpectedAsync(decompiledText, _output);
        }

        [Theory]
        [InlineData("Condition.SimpleSwitch.cs")] // https://github.com/ashmind/SharpLab/issues/25
        //[InlineData("Variable.FromArgumentToCall.cs2cs")] // https://github.com/ashmind/SharpLab/issues/128
        [InlineData("Preprocessor.IfDebug.cs")] // https://github.com/ashmind/SharpLab/issues/161
        [InlineData("Preprocessor.IfDebug.vb")] // https://github.com/ashmind/SharpLab/issues/161
        [InlineData("FSharp/Preprocessor.IfDebug.fs")] // https://github.com/ashmind/SharpLab/issues/161
        [InlineData("Using.Simple.cs")] // https://github.com/ashmind/SharpLab/issues/185
        [InlineData("StringInterpolation.Simple.cs")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode_InDebug(string codeFilePath) {
            var data = await TestCode.FromFileAsync(codeFilePath);
            var driver = await TestDriverFactory.FromCodeAsync(data, Optimize.Debug);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            Assert.True(string.IsNullOrEmpty(errors), errors);
            await data.AssertIsExpectedAsync(decompiledText, _output);
        }

        [Theory]
        [InlineData(LanguageNames.CSharp, "/// <summary><see cref=\"Incorrect\"/></summary>\r\npublic class C {}", "CS1574")] // https://github.com/ashmind/SharpLab/issues/219
        [InlineData(LanguageNames.VisualBasic, "''' <summary><see cref=\"Incorrect\"/></summary>\r\nPublic Class C\r\nEnd Class", "BC42309")]
        public async Task SlowUpdate_ReturnsExpectedWarnings_ForXmlDocumentation(string sourceLanguageName, string code, string expectedWarningId) {
            var driver = TestEnvironment.NewDriver().SetText(code);
            await driver.SendSetOptionsAsync(sourceLanguageName, TargetNames.IL);

            var result = await driver.SendSlowUpdateAsync<string>();
            Assert.Contains(
                new { Severity = "warning", Id = expectedWarningId },
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
            Assert.DoesNotContain(result.Diagnostics, d => d.Severity is "warning" or "error");
        }

        [Theory]
        [InlineData(LanguageNames.CSharp, "CompilerBomb.Generic.1.cs")]
        [InlineData(LanguageNames.CSharp, "CompilerBomb.Generic.2.cs")]
        [InlineData(LanguageNames.VisualBasic, "CompilerBomb.Generic.vb")]
        public async Task SlowUpdate_ReturnsRoslynGuardException_ForCompilerBombs(string languageName, string codeFilePath) {
            var code = await TestCode.FromCodeOnlyFileAsync(codeFilePath);
            var driver = TestEnvironment.NewDriver().SetText(code);
            await driver.SendSetOptionsAsync(languageName, TargetNames.IL);

            await Assert.ThrowsAsync<RoslynCompilationGuardException>(() => driver.SendSlowUpdateAsync<string>());
        }

        [Fact] // https://github.com/ashmind/SharpLab/issues/1232
        public async Task SlowUpdate_ReturnsRoslynGuardException_ForGenericPointerStackOverflow() {
            // TODO: Can be removed once https://github.com/dotnet/roslyn/issues/65594 is resolved
            var code = @"
                using System.ComponentModel;
                class C<T>
                {
                    [DefaultValue(default(C<delegate*<void>[]>.E))]
                    enum E { }
                }
            ";
            var driver = TestEnvironment.NewDriver().SetText(code);
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.IL);

            await Assert.ThrowsAsync<RoslynCompilationGuardException>(() => driver.SendSlowUpdateAsync<string>());
        }

        [Theory]
        [InlineData("x[][][][][]")]
        [InlineData(";[][][][][] x[][][][][]")]
        [InlineData(";[x[][][][][]]")]
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
        [InlineData("; [Attribute1] [Attribute2] [Attribute3] [Attribute4] [Attribute5]")]
        [InlineData("} [Attribute1] [Attribute2] [Attribute3] [Attribute4] [Attribute5]")]
        public async Task SetOptions_ProcessesTokenEdgeCases_WithoutTokenValidationErrors(string code) {
            var driver = TestEnvironment.NewDriver().SetText(code);

            var exception = await Record.ExceptionAsync(() => driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.IL));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData("Attributes.TopLevelSequence.cs")]
        public async Task SetOptions_ProcessesComplexTokenEdgeCases_WithoutTokenValidationErrors(string codeFilePath) {
            var code = await TestCode.FromCodeOnlyFileAsync(codeFilePath);
            var driver = TestEnvironment.NewDriver().SetText(code);

            var exception = await Record.ExceptionAsync(() => driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.IL));

            Assert.Null(exception);
        }

        [Fact] // https://github.com/ashmind/SharpLab/issues/817
        public async Task SlowUpdate_DoesNotReportAnyErrors_WhenSwitchingFromTopLevelStatementsToNonTopLevel() {
            // Arrange
            var code = "class C { void M() {} }";
            var driver = TestEnvironment.NewDriver().SetTextWithCursor(code + "|");
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.CSharp);
            // switches to top-level statement mode
            await driver.SendTypeCharAsync('+');
            await driver.SendSlowUpdateAsync();
            // switches back (removes + at the end)
            await driver.SendBackspaceAsync();

            // Act
            var result = await driver.SendSlowUpdateAsync<string>();

            // Assert
            var errors = result.JoinErrors();
            Assert.True(string.IsNullOrEmpty(errors), errors);
        }
    }
}
