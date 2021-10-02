using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using SharpLab.Server.Common;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Decompilation {
    public class TargetJitAsmTests {
        private readonly ITestOutputHelper _output;

        public TargetJitAsmTests(ITestOutputHelper output) {
            _output = output;
            // TestAssemblyLog.Enable(output);
        }

        [Theory]
        [InlineData("JitAsm.Simple.cs2asm")]
        [InlineData("JitAsm.MultipleReturns.cs2asm")]
        [InlineData("JitAsm.ArrayElement.cs2asm")]
        // TODO: Understand why these tests are flaky and keep switching between
        // resolving and non-resolving symbols. Since it's .NET Framework, low priority.
        //
        // Non-resolving:
        // [InlineData("JitAsm.AsyncRegression.CI.cs2asm")]
        // [InlineData("JitAsm.ConsoleWrite.CI.cs2asm")]
        //
        // Resolving
        // [InlineData("JitAsm.AsyncRegression.cs2asm")]
        // [InlineData("JitAsm.ConsoleWrite.cs2asm")]
        [InlineData("JitAsm.JumpBack.cs2asm")] // https://github.com/ashmind/SharpLab/issues/229
        [InlineData("JitAsm.Delegate.cs2asm")]
        [InlineData("JitAsm.Nested.Simple.cs2asm")]
        [InlineData("JitAsm.Generic.Open.Multiple.cs2asm")]
        [InlineData("JitAsm.Generic.MethodWithAttribute.cs2asm")]
        [InlineData("JitAsm.Generic.ClassWithAttribute.cs")]
        // TODO: Diagnose later
        // [InlineData("JitAsm.Generic.MethodWithAttribute.fs2asm")]
        [InlineData("JitAsm.Generic.Nested.AttributeOnTop.cs")]
        [InlineData("JitAsm.Generic.Nested.AttributeOnNested.cs")]
        [InlineData("JitAsm.Generic.Nested.AttributeOnBoth.cs")]
        [InlineData("JitAsm.DllImport.cs")] // https://github.com/ashmind/SharpLab/issues/666
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
        [InlineData("class C { static int F = 1; }")]
        [InlineData("class C { static C() {} }")]
        [InlineData("class C { class N { static N() {} } }")]
        public async Task SlowUpdate_ReturnsNotSupportedError_ForStaticConstructors(string code) {
            var driver = TestEnvironment.NewDriver().SetText(code);
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.JitAsm);

            await Assert.ThrowsAsync<NotSupportedException>(() => driver.SendSlowUpdateAsync<string>());
        }
    }
}
