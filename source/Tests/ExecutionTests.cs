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

namespace SharpLab.Tests {
    public class ExecutionTests {
        private static readonly MirrorSharpOptions MirrorSharpOptions = Startup.CreateMirrorSharpOptions();

        [Theory]
        [InlineData("Exception.DivideByZero.cs", 4, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.When.True.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.When.False.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Finally.cs", 5, "DivideByZeroException")]
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

        [Theory]
        [InlineData("Loop.For.10Iterations.cs", 3, "i: 0; i: 1; i: 2; …")]
        [InlineData("Variable.MultipleDeclarationsOnTheSameLine.cs", 3, "a: 0, b: 0, c: 0, …")]
        [InlineData("Variable.LongName.cs", 3, "abcdefghi…: 0")]
        [InlineData("Variable.LongValue.cs", 3, "x: 123456789…")]
        public async Task SlowUpdate_ReportsVariableNotesWithLengthLimits(string resourceName, int lineNumber, string expectedNotes) {
            var driver = await NewTestDriverAsync(LoadCodeFromResource(resourceName));

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var errors = result.JoinErrors();

            var notes = string.Join(
                "; ",
                result.ExtensionResult.Flow
                    .Where(s => s.Line == lineNumber && s.Notes != null)
                    .Select(s => s.Notes)
            );

            Assert.True(errors.IsNullOrEmpty(), errors);
            Assert.Equal(expectedNotes, notes);
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
