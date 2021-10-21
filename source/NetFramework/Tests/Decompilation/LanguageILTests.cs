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
            var code = TestCode.FromFile(codeFilePath);
            var driver = await TestDriverFactory.FromCodeAsync(code);

            var result = await driver.SendSlowUpdateAsync<string>();
            var errors = result.JoinErrors();

            var decompiledText = result.ExtensionResult?.Trim();
            Assert.True(string.IsNullOrEmpty(errors), errors);
            code.AssertIsExpected(decompiledText, _output);
        }

        [Fact]
        public async Task SlowUpdate_ReturnsWarningDiagnostic_ForPermissionSetInNewerFormat() {
            // Arrange
            var driver = await TestDriverFactory.FromCodeAsync(@"
                .assembly _
                {
                    .permissionset reqmin = (
                        2e 02 80 84 53 79 73 74 65 6d 2e 53 65 63 75 72
                        69 74 79 2e 50 65 72 6d 69 73 73 69 6f 6e 73 2e
                        53 65 63 75 72 69 74 79 50 65 72 6d 69 73 73 69
                        6f 6e 41 74 74 72 69 62 75 74 65 2c 20 6d 73 63
                        6f 72 6c 69 62 2c 20 56 65 72 73 69 6f 6e 3d 34
                        2e 30 2e 30 2e 30 2c 20 43 75 6c 74 75 72 65 3d
                        6e 65 75 74 72 61 6c 2c 20 50 75 62 6c 69 63 4b
                        65 79 54 6f 6b 65 6e 3d 62 37 37 61 35 63 35 36
                        31 39 33 34 65 30 38 39 15 01 54 02 10 53 6b 69
                        70 56 65 72 69 66 69 63 61 74 69 6f 6e 01 80 84
                        53 79 73 74 65 6d 2e 53 65 63 75 72 69 74 79 2e
                        50 65 72 6d 69 73 73 69 6f 6e 73 2e 53 65 63 75
                        72 69 74 79 50 65 72 6d 69 73 73 69 6f 6e 41 74
                        74 72 69 62 75 74 65 2c 20 6d 73 63 6f 72 6c 69
                        62 2c 20 56 65 72 73 69 6f 6e 3d 34 2e 30 2e 30
                        2e 30 2c 20 43 75 6c 74 75 72 65 3d 6e 65 75 74
                        72 61 6c 2c 20 50 75 62 6c 69 63 4b 65 79 54 6f
                        6b 65 6e 3d 62 37 37 61 35 63 35 36 31 39 33 34
                        65 30 38 39 15 01 54 02 10 53 6b 69 70 56 65 72
                        69 66 69 63 61 74 69 6f 6e 01
                    )
                }
            ", LanguageNames.IL, TargetNames.IL);

            // Act
            var result = await driver.SendSlowUpdateAsync<string>();

            // Assert
            Assert.Equal(
                new[] { ("warning", "IL", "Newer (non-XML) permission set format is not yet supported by this library. This permission set will be ignored.") },
                result.Diagnostics.Select(d => (d.Severity, d.Id, d.Message)).ToArray()
            );
        }

        [Fact]
        public async Task SlowUpdate_ReturnsErrorDiagnostic_ForMalformedPermissionSet() {
            // Arrange
            var driver = await TestDriverFactory.FromCodeAsync(@"
                .assembly _ { .permissionset reqmin = ( 01 ) }
            ", LanguageNames.IL, TargetNames.IL);

            // Act
            var result = await driver.SendSlowUpdateAsync<string>();

            // Assert
            Assert.Equal(
                new[] { ("error", "IL", "Failed to parse permission set XML: Invalid syntax on line 1.") },
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
    }
}
