using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using SharpLab.Tests.Internal;
using SharpLab.Server.Common;
using System.Linq;

namespace SharpLab.Tests.Decompilation {
    public class LanguageILTests {
        private readonly ITestOutputHelper _output;

        public LanguageILTests(ITestOutputHelper output) {
            _output = output;
            // TestAssemblyLog.Enable(output);
        }

        [Theory]
        [InlineData("IL/EmptyMethod.il")]
        [InlineData("IL/UnknownAssembly.ToCSharp.il")]
        public async Task SlowUpdate_ReturnsExpectedDecompiledCode(string codeFilePath) {
            var code = await TestCode.FromFileAsync(codeFilePath);
            var driver = await TestDriverFactory.FromCodeAsync(code);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            Assert.True(string.IsNullOrEmpty(errors), errors);
            code.AssertIsExpected(decompiledText, _output);
        }

        [Theory]
        [InlineData("BaseM")]
        [InlineData("Base::M")]
        public async Task SlowUpdate_ReturnsErrorDiagnostic_ForMethodOverrideOutsideOfType(string baseMethod) {
            // Arrange
            var code = @"
                .assembly _ {
                }

                .method hidebysig newslot virtual 
	                instance void M() cil managed 
                {
	                .override method instance void " + baseMethod + @"()
                    ret
                }
            ";
            var driver = await TestDriverFactory.FromCodeAsync(code, LanguageNames.IL, LanguageNames.IL);

            // Act
            var result = await driver.SendSlowUpdateAsync<string>();

            // Assert
            Assert.Equal(
                new[] { ("error", "IL", "Method 'M' is outside class scope and cannot be an override.") },
                result.Diagnostics.Select(d => (d.Severity, d.Id, d.Message)).ToArray()
            );
        }

        [Fact]
        public async Task SlowUpdate_ReturnsUnsupportedWarningDiagnostic_ForAnyPermissionSet() {
            // Arrange
            var driver = await TestDriverFactory.FromCodeAsync(@"
                .assembly _ { .permissionset reqmin = ( 01 ) }
            ", LanguageNames.IL, TargetNames.IL);

            // Act
            var result = await driver.SendSlowUpdateAsync<string>();

            // Assert
            Assert.Equal(
                new[] { ("warning", "IL", "Code Access Security is not supported on this runtime. This permission set will be ignored.") },
                result.Diagnostics.Select(d => (d.Severity, d.Id, d.Message)).ToArray()
            );
        }

        [Fact]
        public async Task SlowUpdate_ReportsErrorDiagnostic_ForDuplicateMethodDefinition() {
            // Arrange
            var driver = await TestDriverFactory.FromCodeAsync(@"
                .class C
                    extends System.Object
                {
                    .method void M() cil managed {}
                    .method void M() cil managed {}
                }
            ", LanguageNames.IL, TargetNames.IL);

            // Act
            var result = await driver.SendSlowUpdateAsync<string>();

            // Assert
            Assert.Equal(
                new[] { ("error", "IL", "Duplicate method declaration: instance System.Void M()") },
                result.Diagnostics.Select(d => (d.Severity, d.Id, d.Message)).ToArray()
            );
        }

        [Fact]
        public async Task SlowUpdate_ReportsErrorDiagnostic_ForBranchToNonExistentLabel() {
            // Arrange
            var driver = await TestDriverFactory.FromCodeAsync(@"
                .method void M() cil managed
                {
                    br IL_0001
                }
            ", LanguageNames.IL, TargetNames.IL);

            // Act
            var result = await driver.SendSlowUpdateAsync<string>();

            // Assert
            Assert.Equal(
                new[] { ("error", "IL", "Undefined Label:  IL_0001") },
                result.Diagnostics.Select(d => (d.Severity, d.Id, d.Message)).ToArray()
            );
        }
    }
}
