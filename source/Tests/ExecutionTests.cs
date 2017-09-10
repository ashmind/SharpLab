using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using AshMind.Extensions;
using Pedantic.IO;
using MirrorSharp;
using MirrorSharp.Testing;
using MirrorSharp.Testing.Results;
using SharpLab.Server;
using SharpLab.Server.Common;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests {
    public class ExecutionTests {
        private static readonly MirrorSharpOptions MirrorSharpOptions = Startup.CreateMirrorSharpOptions(Startup.CreateContainer());

        [Theory]
        [InlineData("Exception.DivideByZero.cs", 4, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.When.True.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.When.False.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Finally.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.Finally.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.Finally.WriteLine.cs", 5, "DivideByZeroException", Optimize.Debug)]
        [InlineData("Exception.DivideByZero.Catch.Finally.WriteLine.cs", 5, "DivideByZeroException", Optimize.Release)]
        public async Task SlowUpdate_ReportsExceptionInFlow(string resourceName, int expectedLineNumber, string expectedExceptionTypeName, string optimize = Optimize.Debug) {
            var driver = await NewTestDriverAsync(LoadCodeFromResource(resourceName), optimize: optimize);

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var steps = result.ExtensionResult.Flow
                .Select(s => new { s.Line, s.Exception })
                .ToArray();

            AssertIsSuccess(result, allowRuntimeException: true);
            Assert.Contains(new { Line = expectedLineNumber, Exception = expectedExceptionTypeName }, steps);
        }

        [Theory]
        [InlineData("Variable.AssignCall.cs", 3, "x: 0")]
        [InlineData("Variable.ManyVariables.cs", 12, "x10: 0")]
        [InlineData("Return.Simple.cs", 8, "0")]
        public async Task SlowUpdate_ReportsValueNotes(string resourceName, int expectedLineNumber, string expectedNotes) {
            var driver = await NewTestDriverAsync(LoadCodeFromResource(resourceName));

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var steps = result.ExtensionResult.Flow
                .Select(s => new { s.Line, s.Notes })
                .ToArray();

            AssertIsSuccess(result);
            Assert.Contains(new { Line = expectedLineNumber, Notes = expectedNotes }, steps);
        }

        [Theory]
        [InlineData("void M(int a) {}", "M(1)", 1, "a: 1")]
        [InlineData("void M(int a) {\r\n}", "M(1)", 1, "a: 1")]
        [InlineData("void M(int a)\r\n{}", "M(1)", 1, "a: 1", true)]
        [InlineData("void M(int a\r\n) {}", "M(1)", 1, "a: 1", true)]
        [InlineData("void M(\r\nint a\r\n) {}", "M(1)", 2, "a: 1", true)]
        [InlineData("void M(int a) {\r\n\r\nConsole.WriteLine();}", "M(1)", 1, "a: 1")]
        [InlineData("void M(int a, int b) {}", "M(1, 2)", 1, "a: 1, b: 2")]
        [InlineData("void M(int a, out int b) { b = 1; }", "M(1, out var _)", 1, "a: 1")]
        [InlineData("void M(int a, int b = 0) {}", "M(1)", 1, "a: 1, b: 0")]
        public async Task SlowUpdate_ReportsValueNotes_ForCSharpStaticMethodArguments(string methodCode, string methodCallCode, int expectedMethodLineNumber, string expectedNotes, bool expectedSkipped = false) {
            var driver = await NewTestDriverAsync(@"
                using System;
                public static class Program {
                    public static void Main() { " + methodCallCode + @"; }
                    static " + methodCode + @"
                }
            ");
            var methodStartLine = 4; // see above

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var steps = result.ExtensionResult?.Flow
                .Select(s => new { s.Line, s.Notes, s.Skipped })
                .ToArray();

            AssertIsSuccess(result);
            Assert.Contains(new { Line = methodStartLine + expectedMethodLineNumber, Notes = expectedNotes, Skipped = expectedSkipped }, steps);
        }

        [Fact]
        public async Task SlowUpdate_ReportsValueNotes_ForCSharpInstanceMethodArguments() {
            var driver = await NewTestDriverAsync(@"
                using System;
                public class Program {
                    public static void Main() { new Program().M(1); }
                    public void M(int a) {}
                }
            ");

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var steps = result.ExtensionResult?.Flow
                .Select(s => new { s.Line, s.Notes })
                .ToArray();

            AssertIsSuccess(result);
            Assert.Contains(new { Line = 5, Notes = "a: 1" }, steps);
        }

        [Fact]
        public async Task SlowUpdate_ReportsValueNotes_ForCSharpConstructorArguments() {
            var driver = await NewTestDriverAsync(@"
                using System;
                public class Program {
                    Program(int a) {}
                    public static void Main() { new Program(1); }
                }
            ");

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var steps = result.ExtensionResult?.Flow
                .Select(s => new { s.Line, s.Notes })
                .ToArray();

            AssertIsSuccess(result);
            Assert.Contains(new { Line = 4, Notes = "a: 1" }, steps);
        }

        [Theory]
        [InlineData("Loop.For.10Iterations.cs", 3, "i: 0; i: 1; i: 2; …")]
        [InlineData("Variable.MultipleDeclarationsOnTheSameLine.cs", 3, "a: 0, b: 0, c: 0, …")]
        [InlineData("Variable.LongName.cs", 3, "abcdefghi…: 0")]
        [InlineData("Variable.LongValue.cs", 3, "x: 123456789…")]
        public async Task SlowUpdate_ReportsValueNotes_WithLengthLimits(string resourceName, int lineNumber, string expectedNotes) {
            var driver = await NewTestDriverAsync(LoadCodeFromResource(resourceName));

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            var notes = string.Join(
                "; ",
                result.ExtensionResult.Flow
                    .Where(s => s.Line == lineNumber && s.Notes != null)
                    .Select(s => s.Notes)
            );

            AssertIsSuccess(result);
            Assert.Equal(expectedNotes, notes);
        }

        [Fact]
        public async Task SlowUpdate_IncludesReturnValueInOutput() {
            var driver = await NewTestDriverAsync(@"
                public static class Program {
                    public static int Main() { return 3; }
                }
            ");

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result);
            Assert.Equal("Return: 3", result.ExtensionResult.GetOutputAsString());
        }

        [Fact]
        public async Task SlowUpdate_IncludesExceptionInOutput() {
            var driver = await NewTestDriverAsync(@"
                public static class Program {
                    public static int Main() { throw new System.Exception(""Test""); }
                }
            ");

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result, allowRuntimeException: true);
            Assert.Matches("^Exception: System.Exception: Test", result.ExtensionResult.GetOutputAsString());
        }

        [Theory]
        [InlineData("3.Inspect();", "Inspect: 3")]
        [InlineData("(1, 2, 3).Inspect();", "Inspect: (1, 2, 3)")]
        [InlineData("new[] { 1, 2, 3 }.Inspect();", "Inspect: { 1, 2, 3 }")]
        [InlineData("3.Dump();", "Dump: 3")]
        public async Task SlowUpdate_IncludesInspectAndDumpInOutput(string code, string expectedOutput) {
            var driver = await NewTestDriverAsync(@"
                public static class Program {
                    public static void Main() { " + code + @" }
                }
            ");

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result);
            Assert.Equal(expectedOutput, result.ExtensionResult.GetOutputAsString());
        }

        [Theory]
        [InlineData("Console.Write(\"abc\");", "abc")]
        [InlineData("Console.WriteLine(\"abc\");", "abc{newline}")]
        [InlineData("Console.Write('a');", "a")]
        [InlineData("Console.Write(3);", "3")]
        [InlineData("Console.Write(3.1);", "3.1")]
        [InlineData("Console.Write(new object());", "System.Object")]
        public async Task SlowUpdate_IncludesConsoleInOutput(string code, string expectedOutput) {
            var driver = await NewTestDriverAsync(@"
                using System;
                public static class Program {
                    public static void Main() { " + code + @" }
                }
            ");

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result);
            Assert.Equal(
                expectedOutput.Replace("{newline}", Environment.NewLine),
                result.ExtensionResult.GetOutputAsString()
            );
        }

        [Theory]
        [InlineData("Api.Expressions.Simple.cs")]
        public async Task SlowUpdate_AllowsExpectedApis(string resourceName) {
            var driver = await NewTestDriverAsync(LoadCodeFromResource(resourceName));
            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result);
        }

        [Fact]
        public async Task SlowUpdate_ExecutesVisualBasic() {
            var driver = await NewTestDriverAsync(@"
                Imports System
                Public Module Program
                    Public Sub Main()
                        Console.Write(""Test"")
                    End Sub
                End Module
            ", LanguageNames.VisualBasic);

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result);
            Assert.Equal("Test", result.ExtensionResult.GetOutputAsString());
        }

        [Fact]
        public async Task SlowUpdate_ExecutesFSharp() {
            var driver = await NewTestDriverAsync(@"
                open System
                printf ""Test""
            ", "F#");

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result);
            Assert.Equal("Test", result.ExtensionResult.GetOutputAsString());
        }

        [Fact]
        public async Task SlowUpdate_ExecutesFSharp_WithExplicitEntryPoint() {
            var driver = await NewTestDriverAsync(@"
                open System

                [<EntryPoint>]
                let main argv = 
                    printf ""Test""
                    0
            ", "F#");

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result);
            Assert.Equal("Test\nReturn: 0", result.ExtensionResult.GetOutputAsString());
        }

        [Theory]
        [InlineData("Regression.CertainLoop.cs")]
        [InlineData("Regression.FSharpNestedLambda.fs", LanguageNames.FSharp)]
        [InlineData("Regression.NestedAnonymousObject.cs")]
        public async Task SlowUpdate_DoesNotFail(string resourceName, string languageName = LanguageNames.CSharp) {
            var driver = await NewTestDriverAsync(LoadCodeFromResource(resourceName), languageName);
            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            AssertIsSuccess(result);
        }

        private static void AssertIsSuccess(SlowUpdateResult<ExecutionResultData> result, bool allowRuntimeException = false) {
            var errors = result.JoinErrors();
            Assert.True(errors.IsNullOrEmpty(), errors);
            var output = result.ExtensionResult.GetOutputAsString();
            Assert.DoesNotMatch("InvalidProgramException", output);

            if (allowRuntimeException)
                return;
            Assert.DoesNotMatch("Exception:", output);
        }

        private static string LoadCodeFromResource(string resourceName) {
            return EmbeddedResource.ReadAllText(typeof(ExecutionTests), "TestCode.Execution." + resourceName);
        }

        private static async Task<MirrorSharpTestDriver> NewTestDriverAsync(
            string code,
            string languageName = LanguageNames.CSharp,
            string optimize = Optimize.Debug
        ) {
            var driver = MirrorSharpTestDriver.New(MirrorSharpOptions).SetText(code);
            await driver.SendSetOptionsAsync(languageName, TargetNames.Run, optimize);
            return driver;
        }

        private class ExecutionResultData {
            [JsonIgnore]
            public IList<FlowStepData> Flow { get; } = new List<FlowStepData>();
            [JsonProperty("flow")]
            private IList<JToken> FlowRaw { get; } = new List<JToken>();
            [JsonProperty]
            private IList<JToken> Output { get; } = new List<JToken>();

            public string GetOutputAsString() {
                return string.Join("\n", Output.Select(token => {
                    if (token is JObject @object)
                        return @object.Value<string>("title") + ": " + @object.Value<string>("value");
                    return token.Value<string>();
                }));
            }

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
                    Notes = token.Value<string>("notes"),
                    Skipped = token.Value<bool?>("skipped") ?? false,
                };
            }
        }

        private class FlowStepData {
            public int Line { get; set; }
            public string Exception { get; set; }
            public string Notes { get; set; }
            public bool Skipped { get; set; }
        }
    }
}
