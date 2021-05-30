using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SharpLab.Tests.Internal;
using SharpLab.Tests.Of.Container.Internal;
using Xunit;
using Xunit.Abstractions;

namespace SharpLab.Tests.Of.Container {
    [Collection(TestCollectionNames.Execution)]
    public class InspectTests {
        private readonly ITestOutputHelper _testOutputHelper;

        public InspectTests(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData("3.Inspect();", "#{\"type\":\"inspection:simple\",\"title\":\"Inspect\",\"value\":\"3\"}\n")]
        [InlineData("(1, 2, 3).Inspect();", "#{\"type\":\"inspection:simple\",\"title\":\"Inspect\",\"value\":\"(1, 2, 3)\"}\n")]
        [InlineData("new[] { 1, 2, 3 }.Inspect();", "#{\"type\":\"inspection:simple\",\"title\":\"Inspect\",\"value\":\"{ 1, 2, 3 }\"}\n")]
        [InlineData("3.Dump();", "#{\"type\":\"inspection:simple\",\"title\":\"Dump\",\"value\":\"3\"}\n")]
        public async Task Inspect_ProducesExpectedOutput(string code, string expectedOutput) {
            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);

            Assert.Equal(expectedOutput, RemoveFlowJson(output));
        }

        [Theory]
        [InlineData("Container.Inspect.Heap.Simple.cs2output")]
        [InlineData("Container.Inspect.Heap.Struct.cs2output")]
        [InlineData("Container.Inspect.Heap.Struct.Nested.cs2output")]
        [InlineData("Container.Inspect.Heap.Int32.cs2output")]
        //[InlineData("Output.Inspect.Heap.Null.cs2output"/*, true*/)]
        public async Task InspectHeap_ProducesExpectedOutput(string resourceName/*, bool allowExceptions = false*/) {
            var code = TestCode.FromResource(resourceName);

            var output = await ContainerTestDriver.CompileAndExecuteAsync(code.Original);

            code.AssertIsExpected(RemoveFlowJson(output), _testOutputHelper);
        }

        [Theory]
        [InlineData("Container.Inspect.MemoryGraph.Int32.cs2output")]
        [InlineData("Container.Inspect.MemoryGraph.String.cs2output")]
        [InlineData("Container.Inspect.MemoryGraph.Arrays.cs")]
        [InlineData("Container.Inspect.MemoryGraph.Variables.cs2output")]
        [InlineData("Container.Inspect.MemoryGraph.DateTime.cs2output")] // https://github.com/ashmind/SharpLab/issues/379
        [InlineData("Container.Inspect.MemoryGraph.Null.cs2output")]
        public async Task InspectMemoryGraph_ProducesExpectedOutput(string resourceName) {
            var code = TestCode.FromResource(resourceName);

            var output = await ContainerTestDriver.CompileAndExecuteAsync(code.Original);

            code.AssertIsExpected(RemoveFlowJson(output), _testOutputHelper);
        }

        private string RemoveFlowJson(string output) {
            return Regex.Replace(output, "#\\{\"flow\".+$", "", RegexOptions.Singleline);
        }
    }
}
