using System;
using System.Threading.Tasks;
using SharpLab.Container.Manager.Internal;
using SharpLab.Tests.Internal;
using SharpLab.Tests.Of.Container.Internal;
using Xunit;

namespace SharpLab.Tests.Of.Container.Integration {
    [Collection(TestCollectionNames.Execution)]
    public class StatusTests {
        [Fact]
        public async Task Get_ReturnsOK() {
            var driver = new ContainerManagerApiTestDriver();

            var result = await driver.Client.GetAsync("/status");

            Assert.Equal(200, (int)result.StatusCode);
            Assert.Equal("OK", await result.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Get_Returns543_IfContainerPoolFailedToAllocateForFiveMinutes() {
            var driver = new ContainerManagerApiTestDriver {
                Now = DateTime.Now.AddMinutes(-10)
            };
            driver.Service<ContainerPool>().LastContainerPreallocationException = new Exception();

            var result = await driver.Client.GetAsync("/status");

            Assert.Equal(543, (int)result.StatusCode);
            Assert.StartsWith("🪦: Unable to allocate container", await result.Content.ReadAsStringAsync());
        }
    }
}
