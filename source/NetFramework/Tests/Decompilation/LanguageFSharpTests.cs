using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Decompilation;

public class LanguageFSharpTests {
    private readonly ITestOutputHelper _output;

    public LanguageFSharpTests(ITestOutputHelper output) {
        _output = output;
        // TestAssemblyLog.Enable(output);
    }

    [Theory]
    [InlineData("FSharp/EmptyType.fs")]
    [InlineData("FSharp/SimpleMethod.fs")] // https://github.com/ashmind/SharpLab/issues/119
    [InlineData("FSharp/NotNull.fs")]
    public async Task SlowUpdate_ReturnsExpectedDecompiledCode_ForFSharp(string codeFilePath) {
        var code = TestCode.FromFile(codeFilePath);
        var driver = await TestDriverFactory.FromCodeAsync(code);

        var result = await driver.SendSlowUpdateAsync<string>();
        var errors = result.JoinErrors();

        var decompiledText = result.ExtensionResult?.Trim();
        Assert.True(string.IsNullOrEmpty(errors), errors);
        await code.AssertIsExpectedAsync(decompiledText, _output);
    }
}
