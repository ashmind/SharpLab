using System.Threading.Tasks;
using SharpLab.Server.Common;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;
using Xunit;
using Xunit.Abstractions;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class RegressionTests {
        public RegressionTests(ITestOutputHelper output) {
            // TestAssemblyLog.Enable(output);
        }

        [Theory]
        [InlineData("CertainLoop.cs")]
        [InlineData("FSharpNestedLambda.fs", LanguageNames.FSharp)]
        [InlineData("NestedAnonymousObject.cs")]
        [InlineData("RefReturn.cs")]
        [InlineData("RefStructReturningThis.cs")]
        [InlineData("CatchWithNameSameLineAsClosingTryBracket.cs")]
        [InlineData("MoreThanFourArguments.cs")]
        [InlineData("InitOnlyProperty.cs")]
        [InlineData("TopLevelLocalConstant.cs")]
        [InlineData("LambdaParameterList.vb", LanguageNames.VisualBasic)]
        [InlineData("UnsafePointers.cs")]
        [InlineData("UnsafeFunctionPointerCall.cs")]
        [InlineData("DynamicPassedToGeneric.cs")]
        public async Task Execution_DoesNotFail(string codeFileName, string languageName = LanguageNames.CSharp) {
            // Arrange
            var code = await TestCode.FromCodeOnlyFileAsync("Regression/" + codeFileName);

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code, languageName);

            // Assert
            Assert.DoesNotMatch("Exception:", output);
        }

        [Theory]
        [InlineData("digits.Sort((a, b) => a.CompareTo(b));")] // https://github.com/ashmind/SharpLab/issues/411
        [InlineData("digits.Sort(delegate(int a, int b) { return a.CompareTo(b); });")]
        [InlineData("int Compare(int a, int b) => a.CompareTo(b); digits.Sort(Compare);")]
        [InlineData("digits.Find(a => a > 0);")]
        public async Task Execution_DoesNotFail_OnNestedMethodCall_ForCSharp(string callWithAnonymousMethodCode) {
            // Arrange
            var code = @"
                using System.Collections.Generic;
                public static class Program {
                    public static void Main(string[] args) {
                        var digits = new List<int>();
                        " + callWithAnonymousMethodCode + @"
                    }
                }
            ";

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            // Assert
            Assert.DoesNotMatch("Exception:", output);
        }

        [Theory] // https://github.com/ashmind/SharpLab/issues/388
        [InlineData("void M(Span<int> s) {}", "M(new Span<int>())")]
        [InlineData("void M(ref Span<int> s) {}", "var s = new Span<int>(); M(ref s)")]
        [InlineData("void M(ReadOnlySpan<int> s) {}", "M(new ReadOnlySpan<int>())")]
        [InlineData("void M(ref ReadOnlySpan<int> s) {}", "var s = new ReadOnlySpan<int>(); M(ref s)")]
        public async Task SlowUpdate_DoesNotFail_OnSpanArguments(string methodCode, string methodCallCode) {
            // Arrange
            var code = @"
                using System;
                public static class Program {
                    public static void Main() {
                        " + methodCallCode + @";
                    }
                    static " + methodCode + @"
                }
            ";

            // Act
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            // Assert
            Assert.DoesNotMatch("Exception:", output);
        }
    }
}
