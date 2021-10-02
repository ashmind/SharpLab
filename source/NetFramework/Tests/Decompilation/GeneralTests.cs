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
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode(string resourceName) {
            var code = TestCode.FromResource(resourceName);
            var driver = await TestDriverFactory.FromCodeAsync(code);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            Assert.True(string.IsNullOrEmpty(errors), errors);
            code.AssertIsExpected(decompiledText, _output);
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
            var driver = await TestDriverFactory.FromCodeAsync(data, Optimize.Debug);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            Assert.True(string.IsNullOrEmpty(errors), errors);
            data.AssertIsExpected(decompiledText, _output);
        }

        [Theory]
        [InlineData(LanguageNames.CSharp, "/// <summary><see cref=\"Incorrect\"/></summary>\r\npublic class C {}", "CS1574")] // https://github.com/ashmind/SharpLab/issues/219
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
