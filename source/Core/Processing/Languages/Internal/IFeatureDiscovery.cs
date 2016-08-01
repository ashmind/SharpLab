using System.Collections.Generic;

namespace TryRoslyn.Core.Processing.Languages.Internal {
    public interface IFeatureDiscovery {
        IReadOnlyCollection<string> SlowDiscoverAll();
    }
}