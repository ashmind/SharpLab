using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using AshMind.Extensions;
using Pedantic.IO;
using MirrorSharp;
using MirrorSharp.Testing;
using SharpLab.Server;
using SharpLab.Server.MirrorSharp.Internal;
using Newtonsoft.Json.Linq;

namespace SharpLab.Tests {
    public class ExecutionTests {
        private static readonly MirrorSharpOptions MirrorSharpOptions = Startup.CreateMirrorSharpOptions();

        [Theory]
        [InlineData("Exceptions.CatchDivideByZero.cs")]
        public async Task SlowUpdate_ExecutesTryCatchWithoutErrors(string resourceName) {
            var driver = await NewTestDriverAsync(LoadCodeFromResource(resourceName));

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var errors = result.JoinErrors();

            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.True(result.ExtensionResult.Exception.IsNullOrEmpty(), result.ExtensionResult.Exception);
        }

        [Theory]
        [InlineData("Exceptions.CatchDivideByZero.cs", 5, "DivideByZeroException")]
        [InlineData("Exceptions.DivideByZero.cs", 4, "DivideByZeroException")]
        public async Task SlowUpdate_ReportsExceptionInFlow(string resourceName, int expectedLineNumber, string expectedExceptionTypeName) {
            var driver = await NewTestDriverAsync(LoadCodeFromResource(resourceName));

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var lines = result.ExtensionResult.Flow
                .Select(f => new { Line = (f as JObject)?.Value<int>("line") ?? f.Value<int>(), Exception = (f as JObject)?.Value<string>("exception") })
                .ToArray();

            Assert.Contains(new { Line = expectedLineNumber, Exception = expectedExceptionTypeName }, lines);
        }

        private static string LoadCodeFromResource(string resourceName) {
            return EmbeddedResource.ReadAllText(typeof(ExecutionTests), "TestCode.Execution." + resourceName);
        }

        private static async Task<MirrorSharpTestDriver> NewTestDriverAsync(string code) {
            var driver = MirrorSharpTestDriver.New(MirrorSharpOptions).SetText(code);
            await driver.SendSetOptionsAsync(new Dictionary<string, string> {
                { "optimize", "debug" },
                { "x-target", TargetNames.Run }
            });
            return driver;
        }

        private class ExecutionResultData {
            public string Exception { get; set; }
            public IList<JToken> Flow { get; } = new List<JToken>();
        }        
    }
}
