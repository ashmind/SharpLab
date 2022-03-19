using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using SharpLab.Tests.Internal;
using SharpLab.Server.Common;
using System.Linq;
using System.IO;
using System;

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
        public async Task SlowUpdate_ReportsErrorDiagnostic_ForDuplicateMethodDefinition_AtTopLevel() {
            // Arrange
            var driver = await TestDriverFactory.FromCodeAsync(@"
                .method void M() cil managed {}
                .method void M() cil managed {}
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
        public async Task SlowUpdate_ReportsErrorDiagnostic_ForDuplicateFieldDefinition() {
            // Arrange
            var driver = await TestDriverFactory.FromCodeAsync(@"
                .class C
                    extends System.Object
                {
                    .field int32 f
                    .field int32 f
                }
            ", LanguageNames.IL, TargetNames.IL);

            // Act
            var result = await driver.SendSlowUpdateAsync<string>();

            // Assert
            Assert.Equal(
                new[] { ("error", "IL", "Duplicate field declaration: System.Int32 f") },
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

        [Fact]
        public async Task SlowUpdate_ReportsErrorDiagnostic_ForIncompleteMethod() {
            // Arrange
            var driver = await TestDriverFactory.FromCodeAsync(@".method", LanguageNames.IL, TargetNames.IL);

            // Act
            var result = await driver.SendSlowUpdateAsync<string>();

            // Assert
            Assert.Equal(
                new[] { ("error", "IL", "Unexpected end of file") },
                result.Diagnostics.Select(d => (d.Severity, d.Id, d.Message)).ToArray()
            );
        }

        [Theory]
        [InlineData("SharpLab.Tests.dll", false)]
        [InlineData("SharpLab.Tests.dll", true)]
        public async Task SlowUpdate_ReportsErrorDiagnostic_ForManifestFileAccess(string path, bool useAbsolutePath) {
            // Arrange
            if (useAbsolutePath)
                path = Path.Combine(AppContext.BaseDirectory, path);
            // it is important that the file exists, non-existent file will obviously not be included
            if (!File.Exists(path)) throw new($"File {path} was not found");
            var driver = await TestDriverFactory.FromCodeAsync($@".mresource public '{path.Replace(@"\", @"\\")}' {{}}", LanguageNames.IL, TargetNames.IL);

            // Act
            var result = await driver.SendSlowUpdateAsync<string>();

            // Assert
            Assert.Equal(
                new[] { ("error", "IL", $"Resource file '{path}' does not exist or cannot be accessed from SharpLab") },
                result.Diagnostics.Select(d => (d.Severity, d.Id, d.Message)).ToArray()
            );
        }

        [Theory]
        [InlineData("ldc.r4 ()", "Byte array argument of ldc.r4 must include at least 4 bytes")]
        [InlineData("ldc.r4 (01 02 03)", "Byte array argument of ldc.r4 must include at least 4 bytes")]
        [InlineData("ldc.r8 ()", "Byte array argument of ldc.r8 must include at least 8 bytes")]
        [InlineData("ldc.r8 (01 02 03 04 05 06 07)", "Byte array argument of ldc.r8 must include at least 8 bytes")]
        public async Task SlowUpdate_ReportsErrorDiagnostic_ForLdcFloatBytesWithInsufficientLength(string ldc, string expectedError) {
            // Arrange
            var driver = await TestDriverFactory.FromCodeAsync(@"
                .method void M() cil managed
                {
                    " + ldc + @"
                    ret
                }
            ", LanguageNames.IL, TargetNames.IL);

            // Act
            var result = await driver.SendSlowUpdateAsync<string>();

            // Assert
            Assert.Equal(
                new[] { ("error", "IL", expectedError) },
                result.Diagnostics.Select(d => (d.Severity, d.Id, d.Message)).ToArray()
            );
        }

        [Fact]
        public async Task SlowUpdate_ReportsErrorDiagnostic_ForLocalListStartingWithComma() {
            // Arrange
            var driver = await TestDriverFactory.FromCodeAsync(@"
                .method void M() cil managed
                {
                    .locals init ( ,int32 a )
                    ret
                }
            ", LanguageNames.IL, TargetNames.IL);

            // Act
            var result = await driver.SendSlowUpdateAsync<string>();

            // Assert
            Assert.Equal(
                new[] { ("error", "IL", "Unexpected syntax: missing first item") },
                result.Diagnostics.Select(d => (d.Severity, d.Id, d.Message)).ToArray()
            );
        }
    }
}
