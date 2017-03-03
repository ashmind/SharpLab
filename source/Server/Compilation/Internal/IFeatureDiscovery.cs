using System.Collections.Generic;

namespace TryRoslyn.Server.Compilation.Internal {
    public interface IFeatureDiscovery {
        IReadOnlyCollection<string> SlowDiscoverAll();
    }
}