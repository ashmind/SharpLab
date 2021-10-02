using System;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using SharpLab.Runtime.Internal;
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
        [InlineData("JitAsm/Simple.cs2asm")]
        [InlineData("JitAsm/MultipleReturns.cs2asm")]
        [InlineData("JitAsm/ArrayElement.cs2asm")]
        [InlineData("JitAsm/AsyncRegression.cs2asm")]
        [InlineData("JitAsm/ConsoleWrite.cs2asm")]
        [InlineData("JitAsm/JumpBack.cs2asm")] // https://github.com/ashmind/SharpLab/issues/229
        [InlineData("JitAsm/Delegate.cs2asm")]
        [InlineData("JitAsm/Nested.Simple.cs2asm")]
        [InlineData("JitAsm/Generic.Open.Multiple.cs2asm")]
        [InlineData("JitAsm/Generic.MethodWithAttribute.cs2asm")]
        [InlineData("JitAsm/Generic.ClassWithAttribute.cs")]
        // TODO: Diagnose later
        // [InlineData("JitAsm/Generic.MethodWithAttribute.fs2asm")]
        [InlineData("JitAsm/Generic.Nested.AttributeOnTop.cs")]
        [InlineData("JitAsm/Generic.Nested.AttributeOnNested.cs")]
        [InlineData("JitAsm/Generic.Nested.AttributeOnBoth.cs")]
        [InlineData("JitAsm/Vectors.Avx2.cs2asm")]
        [InlineData("JitAsm/Math.FusedMultiplyAdd.Fma.cs2asm")]
        [InlineData("JitAsm/DllImport.cs")] // https://github.com/ashmind/SharpLab/issues/666
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode(string codeFilePath) {
            // https://github.com/ashmind/SharpLab/issues/514
            if (codeFilePath.Contains(".Fma.") && !Fma.IsSupported)
                codeFilePath = codeFilePath.Replace(".Fma.", ".NoFma.");
            if (codeFilePath.Contains(".Avx2.") && !Avx2.IsSupported)
                codeFilePath = codeFilePath.Replace(".Avx2.", ".NoAvx2.");

            var code = await TestCode.FromFileAsync(codeFilePath);
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
    }
}
