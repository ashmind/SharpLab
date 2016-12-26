using System.Collections.Generic;

namespace TryRoslyn.Core.Compilation.Internal {
    public interface IFeatureDiscovery {
        IReadOnlyCollection<string> SlowDiscoverAll();
    }
}