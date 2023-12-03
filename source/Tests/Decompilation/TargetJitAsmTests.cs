using System;
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
            // TestDiagnosticLog.Enable(output);
        }

        [Theory]
        [InlineData("JitAsm/Simple.cs")]
        [InlineData("JitAsm/MultipleReturns.cs")]
        [InlineData("JitAsm/ArrayElement.cs")]
        [InlineData("JitAsm/AsyncRegression.cs")]
        [InlineData("JitAsm/ConsoleWrite.cs")]
        [InlineData("JitAsm/JumpBack.cs")] // https://github.com/ashmind/SharpLab/issues/229
        [InlineData("JitAsm/Delegate.cs")]
        [InlineData("JitAsm/Nested.Simple.cs")]
        [InlineData("JitAsm/Generic.Open.Multiple.cs")]
        [InlineData("JitAsm/Generic.MethodWithAttribute.cs")]
        [InlineData("JitAsm/Generic.ClassWithAttribute.cs")]
        // TODO: Diagnose later
        // [InlineData("JitAsm/Generic.MethodWithAttribute.fs2asm")]
        [InlineData("JitAsm/Generic.Nested.AttributeOnTop.cs")]
        [InlineData("JitAsm/Generic.Nested.AttributeOnNested.cs")]
        [InlineData("JitAsm/Generic.Nested.AttributeOnBoth.cs")]
        [InlineData("JitAsm/Vectors.Avx2.cs")]
        [InlineData("JitAsm/Math.FusedMultiplyAdd.cs")]
        [InlineData("JitAsm/DllImport.cs")] // https://github.com/ashmind/SharpLab/issues/666
        [InlineData("JitAsm/MethodImpl.InternalCall.cs")] // https://github.com/ashmind/SharpLab/issues/752
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
        [InlineData("class C { static int F = ((Func<int>)(() => throw new ConstructorRanException()))(); }")]
        [InlineData("class C { static C() => throw new ConstructorRanException(); }")]
        [InlineData("class C { class N { static N() => throw new ConstructorRanException(); } }")]
        public async Task SlowUpdate_ReturnsNotSupportedError_ForStaticConstructors(string code) {
            var driver = TestEnvironment.NewDriver().SetText(@$"
                using System;
                public class ConstructorRanException: Exception {{}}

                {code}
            ");
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.JitAsm);

            var (result, exception) = await RecordExceptionOrResultAsync(() => driver.SendSlowUpdateAsync<string>());

            Assert.Empty(result?.JoinErrors() ?? "");
            Assert.IsType<NotSupportedException>(exception);
        }

        [Theory]
        [InlineData("class C { [ModuleInitializer] public static void I() => throw new InitializerRanException(); }")]
        [InlineData("class C { public class N { [ModuleInitializer] public static void I() => throw new InitializerRanException(); } }")]
        [InlineData(@"
            class C { [ModuleInitializer] public static void I() => throw new InitializerRanException(); }
            namespace System.Runtime.CompilerServices {
                public class ModuleInitializerAttribute : Attribute {}
            }
        ")]
        public async Task SlowUpdate_ReturnsNotSupportedError_ForModuleInitializers(string code) {
            var driver = TestEnvironment.NewDriver().SetText(@$"
                using System;
                using System.Runtime.CompilerServices;
                public class InitializerRanException: Exception {{}}

                {code}
            ");
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.JitAsm);

            var (result, exception) = await RecordExceptionOrResultAsync(() => driver.SendSlowUpdateAsync<string>());

            Assert.Empty(result?.JoinErrors() ?? "");
            Assert.IsType<NotSupportedException>(exception);
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

        private async Task<(T? result, Exception? exception)> RecordExceptionOrResultAsync<T>(Func<Task<T>> callAsync) {
            try {
                return (await callAsync(), null);
            }
            catch (Exception ex) {
                return (default, ex);
            }
        }
    }
}
