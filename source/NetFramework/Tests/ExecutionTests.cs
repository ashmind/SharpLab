using System;
using System.Collections.Generic;
#if DEBUG
using System.IO;
#endif
using System.Linq;
#if DEBUG
using System.Reflection;
#endif
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using AshMind.Extensions;
using Pedantic.IO;
using MirrorSharp.Testing;
using MirrorSharp.Testing.Results;
using SharpLab.Server.Common;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests {
    public class ExecutionTests {
        private readonly ITestOutputHelper _testOutputHelper;

        public ExecutionTests(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;

            #if DEBUG
            var testName = ((ITest)
                _testOutputHelper
                    .GetType()
                    .GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(_testOutputHelper)!
            ).DisplayName.Replace(GetType().FullName + ".", "");
            var safeTestName = Regex.Replace(testName, "[^a-zA-Z._-]+", "_");
            if (safeTestName.Length > 100)
                safeTestName = safeTestName.Substring(0, 100) + "-" + safeTestName.GetHashCode();

            var testPath = Path.Combine(
                AppContext.BaseDirectory, "assembly-log",
                GetType().Name, safeTestName,
                "{0}.dll"
            );
            //AssemblyLog.Enable(testPath);
            #endif
        }

        [Theory]
        [InlineData("Exception.DivideByZero.cs", 4, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.When.True.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.When.False.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Finally.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.Finally.cs", 5, "DivideByZeroException")]
        [InlineData("Exception.DivideByZero.Catch.Finally.WriteLine.cs", 5, "DivideByZeroException", Optimize.Debug)]
        [InlineData("Exception.DivideByZero.Catch.Finally.WriteLine.cs", 5, "DivideByZeroException", Optimize.Release)]
        [InlineData("Exception.Throw.New.Finally.cs", 8, "Exception", Optimize.Debug)]
        public async Task SlowUpdate_ReportsExceptionInFlow(string resourceName, int expectedLineNumber, string expectedExceptionTypeName, string optimize = Optimize.Debug) {
            var driver = await NewTestDriverAsync(LoadCodeFromResource(resourceName), optimize: optimize);

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var steps = result.ExtensionResult!.Flow
                .Select(s => new { s.Line, s.Exception })
                .ToArray();

            AssertIsSuccess(result, allowRuntimeException: true);
            Assert.Contains(new { Line = expectedLineNumber, Exception = expectedExceptionTypeName }, steps);
        }

        [Theory]
        [InlineData("Notes.Variable.AssignCall.cs")]
        [InlineData("Notes.Variable.ManyVariables.cs")]
        [InlineData("Notes.Return.Simple.cs")]
        [InlineData("Notes.Return.Ref.cs")]
        [InlineData("Notes.Return.Ref.Readonly.cs")]
        [InlineData("Notes.Loop.For.10Iterations.cs")]
        [InlineData("Notes.Variable.MultipleDeclarationsOnTheSameLine.cs")]
        [InlineData("Notes.Variable.LongName.cs")]
        [InlineData("Notes.Variable.LongValue.cs")]
        [InlineData("Notes.Regression.ToStringNull.cs")] // https://github.com/ashmind/SharpLab/issues/380
        public async Task SlowUpdate_ReportsValueNotes(string resourceName) {
            var code = LoadCodeFromResource(resourceName);
            var expected = code.Split("\r\n").Select((line, index) => new {
                Line = index + 1,
                Notes = Regex.Match(line, @"//\s+\[(.+)\]\s*$").Groups[1].Value
            }).Where(x => !string.IsNullOrEmpty(x.Notes)).ToArray();

            var driver = await NewTestDriverAsync(code);

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var steps = result.ExtensionResult?.Flow
                .Select(s => new { s.Line, s.Notes })
                .Where(s => !string.IsNullOrEmpty(s.Notes))
                .GroupBy(s => s.Line)
                .Select(g => new { Line = g.Key, Notes = string.Join("; ", g.Select(s => s.Notes)) })
                .ToArray();

            AssertIsSuccess(result);
            Assert.Equal(expected, steps);
        }

        [Theory]
        [InlineData("void M(int a) {}", "M(1)", 1, "a: 1")]
        [InlineData("void M(int a) {\r\n}", "M(1)", 1, "a: 1")]
        [InlineData("void M(int a)\r\n{}", "M(1)", 1, "a: 1", true)]
        [InlineData("void M(int a\r\n) {}", "M(1)", 1, "a: 1", true)]
        [InlineData("void M(\r\nint a\r\n) {}", "M(1)", 2, "a: 1", true)]
        [InlineData("void M(int a) {\r\n\r\nConsole.WriteLine();}", "M(1)", 1, "a: 1")]
        [InlineData("void M(in int a) {}", "M(1)", 1, "a: 1")]
        [InlineData("void M(ref int a) {}", "int x = 1; M(ref x)", 1, "a: 1")]
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

        [Fact]
        public async Task SlowUpdate_IncludesReturnValueInOutput() {
            var driver = await NewTestDriverAsync(@"
                public static class Program {
                    public static int Main() { return 3; }
                }
            ");

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result);
            Assert.Equal("Return: 3", result.ExtensionResult?.GetOutputAsString());
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
            Assert.Matches("^Exception: System.Exception: Test", result.ExtensionResult?.GetOutputAsString());
        }

        [Theory]
        [InlineData("3.Inspect();", "Inspect: 3")]
        [InlineData("(1, 2, 3).Inspect();", "Inspect: (1, 2, 3)")]
        [InlineData("new[] { 1, 2, 3 }.Inspect();", "Inspect: { 1, 2, 3 }")]
        [InlineData("3.Dump();", "Dump: 3")]
        public async Task SlowUpdate_IncludesSimpleInspectAndDumpInOutput(string code, string expectedOutput) {
            var driver = await NewTestDriverAsync(@"
                public static class Program {
                    public static void Main() { " + code + @" }
                }
            ");

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result);
            Assert.Equal(expectedOutput, result.ExtensionResult?.GetOutputAsString());
        }

        [Theory]
        [InlineData("Output.Inspect.Heap.Simple.cs2output")]
        [InlineData("Output.Inspect.Heap.Struct.cs2output")]
        [InlineData("Output.Inspect.Heap.Struct.Nested.cs2output")]
        [InlineData("Output.Inspect.Heap.Int32.cs2output")]
        [InlineData("Output.Inspect.Heap.Null.cs2output", true)]
        public async Task SlowUpdate_IncludesInspectHeapInOutput(string resourceName, bool allowExceptions = false) {
            var code = TestCode.FromResource("Execution." + resourceName);
            var driver = await NewTestDriverAsync(code.Original);

            var result = await SendSlowUpdateWithRetryOnMovedObjectsAsync(driver);

            AssertIsSuccess(result, allowRuntimeException: allowExceptions);
            code.AssertIsExpected(result.ExtensionResult?.GetOutputAsString(), _testOutputHelper);
        }

        [Theory]
        [InlineData("Output.Inspect.MemoryGraph.Int32.cs2output")]
        [InlineData("Output.Inspect.MemoryGraph.String.cs2output")]
        [InlineData("Output.Inspect.MemoryGraph.Arrays.cs2output")]
        [InlineData("Output.Inspect.MemoryGraph.Variables.cs2output")]
        [InlineData("Output.Inspect.MemoryGraph.DateTime.cs2output")] // https://github.com/ashmind/SharpLab/issues/379
        [InlineData("Output.Inspect.MemoryGraph.Null.cs2output")]
        public async Task SlowUpdate_IncludesInspectMemoryGraphInOutput(string resourceName) {
            var code = TestCode.FromResource("Execution." + resourceName);
            var driver = await NewTestDriverAsync(code.Original);

            var result = await SendSlowUpdateWithRetryOnMovedObjectsAsync(driver);

            AssertIsSuccess(result);
            code.AssertIsExpected(result.ExtensionResult?.GetOutputAsString(), _testOutputHelper);
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
                result.ExtensionResult?.GetOutputAsString()
            );
        }

        [Fact]
        public async Task SlowUpdate_DoesNotIncludePreviousConsoleOutput_IfRunTwice() {
            var driver = await NewTestDriverAsync(@"
                using System;
                public static class Program {
                    public static void Main() { Console.Write('I'); }
                }
            ");

            await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result);
            Assert.Equal("I", result.ExtensionResult?.GetOutputAsString());
        }

        [Theory]
        [InlineData("Console.Write(3.1);", "cs-CZ", "3.1")]
        public async Task SlowUpdate_IncludesConsoleInOutput_UsingInvariantCulture(string code, string currentCultureName, string expectedOutput) {
            var driver = await NewTestDriverAsync(@"
                using System;
                using System.Globalization;
                public static class Program {
                    public static void Main() {
                        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(""" + currentCultureName + @""");
                        " + code + @"
                    }
                }
            ");

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result);
            Assert.Equal(
                expectedOutput.Replace("{newline}", Environment.NewLine),
                result.ExtensionResult?.GetOutputAsString()
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
            Assert.Equal("Test", result.ExtensionResult?.GetOutputAsString());
        }

        [Fact]
        public async Task SlowUpdate_ExecutesFSharp() {
            var driver = await NewTestDriverAsync(@"
                open System
                printf ""Test""
            ", "F#");

            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result);
            Assert.Equal("Test", result.ExtensionResult?.GetOutputAsString());
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
            Assert.Equal("Test\nReturn: 0", result.ExtensionResult?.GetOutputAsString());
        }

        [Theory]
        [InlineData("Regression.CertainLoop.cs")]
        [InlineData("Regression.FSharpNestedLambda.fs", LanguageNames.FSharp)]
        [InlineData("Regression.NestedAnonymousObject.cs")]
        [InlineData("Regression.ReturnRef.cs")]
        public async Task SlowUpdate_DoesNotFail(string resourceName, string languageName = LanguageNames.CSharp) {
            var driver = await NewTestDriverAsync(LoadCodeFromResource(resourceName), languageName);
            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            AssertIsSuccess(result);
        }

        [Theory] // https://github.com/ashmind/SharpLab/issues/388
        [InlineData("void M(Span<int> s) {}", "M(new Span<int>())")]
        [InlineData("void M(ref Span<int> s) {}", "var s = new Span<int>(); M(ref s)")]
        [InlineData("void M(ReadOnlySpan<int> s) {}", "M(new ReadOnlySpan<int>())")]
        [InlineData("void M(ref ReadOnlySpan<int> s) {}", "var s = new ReadOnlySpan<int>(); M(ref s)")]
        public async Task SlowUpdate_DoesNotFail_OnSpanArguments(string methodCode, string methodCallCode) {
            var driver = await NewTestDriverAsync(@"
                using System;
                public static class Program {
                    public static void Main() {
                        " + methodCallCode + @";
                    }
                    static " + methodCode + @"
                }
            ");
            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            AssertIsSuccess(result);
        }

        [Theory]
        [InlineData("digits.Sort((a, b) => a.CompareTo(b));")] // https://github.com/ashmind/SharpLab/issues/411
        [InlineData("digits.Sort(delegate(int a, int b) { return a.CompareTo(b); });")]
        [InlineData("int Compare(int a, int b) => a.CompareTo(b); digits.Sort(Compare);")]
        [InlineData("digits.Find(a => a > 0);")]
        public async Task SlowUpdate_DoesNotFail_OnNestedMethodCall_ForCSharp(string callWithAnonymousMethodCode) {
            var driver = await NewTestDriverAsync(@"
                using System.Collections.Generic;
                public static class Program {
                    public static void Main(string[] args) {
                        var digits = new List<int>();
                        " + callWithAnonymousMethodCode + @"
                    }
                }
            ");
            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            AssertIsSuccess(result);
        }

        [Fact] // https://github.com/ashmind/SharpLab/issues/411
        public async Task SlowUpdate_DoesNotFail_OnLambdaParameterList_ForVisualBasic() {
            var driver = await NewTestDriverAsync(@"
                Imports System.Collections.Generic
                Public Module Program
                    Public Sub Main(ByVal args() As String)
                        Dim list as New List(of Integer)
                        list.Sort(Function(a, b) a.CompareTo(b))
                    End Sub
                End Module
            ", LanguageNames.VisualBasic);
            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            AssertIsSuccess(result);
        }

        [Theory]
        [InlineData("Regression.Disposable.cs")]
        public async Task SlowUpdate_DoesNotFail_OnAnyGuard(string resourceName) {
            var driver = await NewTestDriverAsync(LoadCodeFromResource(resourceName), LanguageNames.CSharp);
            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();

            AssertIsSuccess(result, allowRuntimeException: true);
            Assert.DoesNotMatch("GuardException", result.ExtensionResult?.GetOutputAsString());
        }

        // Currently Inspect.Heap/MemoryGraph does not promise to always work as expected if GCs happen
        // during its operation. So for now we retry in the tests.
        private static async Task<SlowUpdateResult<ExecutionResultData>> SendSlowUpdateWithRetryOnMovedObjectsAsync(MirrorSharpTestDriver driver) {
            var result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
            var tryCount = 1;
            while (result.JoinErrors().Contains("Failed to find object type for address") && tryCount < 10) {
                result = await driver.SendSlowUpdateAsync<ExecutionResultData>();
                tryCount += 1;
            }
            return result!;
        }

        private static void AssertIsSuccess(SlowUpdateResult<ExecutionResultData> result, bool allowRuntimeException = false) {
            var errors = result.JoinErrors();
            Assert.True(string.IsNullOrEmpty(errors), errors);
            var output = result.ExtensionResult?.GetOutputAsString();
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
            var driver = TestEnvironment.NewDriver().SetText(code);
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
                        return ConvertOutputObjectToString(@object);
                    return token.Value<string>();
                }));
            }

            private string ConvertOutputObjectToString(JObject @object) {
                if (@object.Value<string>("type") == "inspection:simple")
                    return @object.Value<string>("title") + ": " + @object.Value<string>("value");
                return @object.ToString();
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
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
            public int Line { get; set; }
            public string Exception { get; set; }
            public string Notes { get; set; }
            public bool Skipped { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        }
    }
}