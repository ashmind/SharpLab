using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TryRoslyn.Core.Processing.Languages.Internal;
using Xunit;

namespace TryRoslyn.Tests {
    public class FeatureDiscoveryTests {
        [Fact]
        public void CSharpDiscoverAll_ReturnsExpectedFeature() {
            Assert.Contains("IOperation", new CSharpFeatureDiscovery().SlowDiscoverAll());
        }

        [Fact]
        public void VBNetDiscoverAll_ReturnsExpectedFeature() {
            Assert.Contains("IOperation", new VBNetFeatureDiscovery().SlowDiscoverAll());
        }
    }
}
