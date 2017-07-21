using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Xunit;
using AshMind.Extensions;
using Pedantic.IO;
using MirrorSharp;
using MirrorSharp.Testing;
using SharpLab.Server;
using SharpLab.Server.MirrorSharp.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace SharpLab.Tests {
    public class ExecutionTests {
        private static readonly MirrorSharpOptions MirrorSharpOptions = Startup.CreateMirrorSharpOptions();

        [Theory]
        [InlineData("Exceptions.DivideByZero.cs", 4, "DivideByZeroException")]
        [InlineData("Exceptions.DivideByZero.Catch.cs", 5, "DivideByZeroException")]
        [InlineData("Exceptions.DivideByZero.Catch.When.True.cs", 5, "DivideByZeroException")]
        [InlineData("Exceptions.DivideByZero.Catch.When.False.cs", 5, "DivideByZeroException")]
        [InlineData("Exceptions.DivideByZero.Finally.cs", 5, "DivideByZeroException")]
        public async Task SlowUpdate_ReportsExceptionInFlow(string resourceName, int expectedLineNumber, string expectedExceptionTypeName) {
            var driver = await NewTestDriverAsync(LoadCodeFromResource(resourceName));

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var errors = result.JoinErrors();
            var steps = result.ExtensionResult.Flow
                .Select(s => new { s.Line, s.Exception })
                .ToArray();

            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Contains(new { Line = expectedLineNumber, Exception = expectedExceptionTypeName }, steps);
        }

        [Fact]
        public async Task SlowUpdate_ReportsLimitedNumberOfNotesPerLine() {
            var driver = await NewTestDriverAsync(LoadCodeFromResource("Loops.For.10Iterations.cs"));

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var errors = result.JoinErrors();

            var notes = string.Join(
                ", ",
                result.ExtensionResult.Flow
                    .Where(s => s.Line == 3 && s.Notes != null)
                    .Select(s => s.Notes)
            );

            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal("i: 0, i: 1, i: 2, …", notes);
        }

        private static int LineNumberFromFlowStep(JToken step) {
            return (step as JObject)?.Value<int>("line") ?? step.Value<int>();
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
            [JsonIgnore]
            public IList<FlowStepData> Flow { get; } = new List<FlowStepData>();
            [JsonProperty("flow")]
            private IList<JToken> FlowRaw { get; } = new List<JToken>();

            [OnDeserialized]
            private void OnDeserialized(StreamingContext context) {
                foreach (var token in FlowRaw) {
                    Flow.Add(ParseStepData(token));
                }
            }

            private FlowStepData ParseStepData(JToken token) {
                if (token is JValue value)
                    return new FlowStepData { Line = value.Value<int>() };

                return new FlowStepData {
                    Line = token.Value<int>("line"),
                    Exception = token.Value<string>("exception"),
                    Notes = token.Value<string>("notes")
                };
            }
        }

        private class FlowStepData {
            public int Line { get; set; }
            public string Exception { get; set; }
            public string Notes { get; set; }
        }
    }
}
