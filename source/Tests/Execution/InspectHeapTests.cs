using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class InspectHeapTests {
        private readonly ITestOutputHelper _testOutputHelper;

        public InspectHeapTests(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData("Simple.cs2output")]
        [InlineData("Struct.cs2output")]
        [InlineData("Struct.Nested.cs2output")]
        [InlineData("Int32.cs2output")]
        //[InlineData("Null.cs2output"/*, true*/)]
        public async Task InspectHeap_ProducesExpectedOutput(string resourceName/*, bool allowExceptions = false*/) {
            var code = await TestCode.FromFileAsync("Inspect/Heap/" + resourceName);

            var output = await ContainerTestDriver.CompileAndExecuteAsync(code.Original);

            code.AssertIsExpected(TestOutput.RemoveFlowJson(output), _testOutputHelper);
        }
    }
}
