using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MirrorSharp.Testing;

namespace SharpLab.Tests.Internal {
    public static class MirrorSharpTestDriverExtensions {
        public static Task SendSetOptionsAsync(this MirrorSharpTestDriver driver, string sourceLanguageName, string targetName, OptimizationLevel optimizationLevel = OptimizationLevel.Release) {
            return driver.SendSetOptionsAsync(new Dictionary<string, string> {
                {"language", sourceLanguageName},
                {"optimize", optimizationLevel.ToString().ToLowerInvariant()},
                {"x-target", targetName}
            });
        }
    }
}
