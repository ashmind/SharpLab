using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class InspectMemoryGraphTests {
        private readonly ITestOutputHelper _testOutputHelper;

        public InspectMemoryGraphTests(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData("Int32.cs")]
        [InlineData("String.cs")]
        [InlineData("Arrays.cs")]
        [InlineData("Variables.cs")]
        [InlineData("DateTime.cs")] // https://github.com/ashmind/SharpLab/issues/379
        [InlineData("Null.cs")]
        public async Task InspectMemoryGraph_ProducesExpectedOutput(string resourceName) {
            var code = await TestCode.FromFileAsync("Inspect/MemoryGraph/" + resourceName);

            var output = await ContainerTestDriver.CompileAndExecuteAsync(code.Original);

            await code.AssertIsExpectedAsync(TestOutput.RemoveFlowJson(output), _testOutputHelper);
        }
    }
}
