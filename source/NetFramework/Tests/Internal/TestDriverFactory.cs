using System.Threading.Tasks;
using MirrorSharp.Testing;
using SharpLab.Server.Common;

namespace SharpLab.Tests.Internal {
    public static class TestDriverFactory {
        public static async Task<MirrorSharpTestDriver> FromCodeAsync(TestCode code, string optimize = Optimize.Release) {
            var driver = TestEnvironment.NewDriver();
            await driver.SendSetOptionsAsync(code.SourceLanguageName, code.TargetName, optimize);
            driver.SetText(code.Original);
            return driver;
        }

        public static async Task<MirrorSharpTestDriver> FromCode(string code, string sourceLanguageName, string targetName) {
            var driver = TestEnvironment.NewDriver().SetText(code);
            await driver.SendSetOptionsAsync(sourceLanguageName, targetName);
            return driver;
        }
    }
}
