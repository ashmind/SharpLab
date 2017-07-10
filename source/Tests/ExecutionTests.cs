using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using AshMind.Extensions;
using Pedantic.IO;
using MirrorSharp;
using MirrorSharp.Testing;
using SharpLab.Server;
using SharpLab.Server.MirrorSharp.Internal;

namespace SharpLab.Tests {
    public class ExecutionTests {
        private static readonly MirrorSharpOptions MirrorSharpOptions = Startup.CreateMirrorSharpOptions();

        [Theory]
        [InlineData("Exceptions.CatchDivideByZero.cs")]
        public async Task SlowUpdate_ExecutesTryCatchWithoutErrors(string resourceName) {
            var code = EmbeddedResource.ReadAllText(typeof(ExecutionTests), "TestCode.Execution." + resourceName);
            var driver = MirrorSharpTestDriver.New(MirrorSharpOptions).SetText(code);
            await driver.SendSetOptionsAsync(new Dictionary<string, string> {
                { "optimize", "debug" },
                { "x-target", TargetNames.Run }
            });

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var errors = result.JoinErrors();

            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.True(result.ExtensionResult.Exception.IsNullOrEmpty(), result.ExtensionResult.Exception);
        }

        private class ExecutionResultData {
            public string Exception { get; set; }
        }
    }
}
