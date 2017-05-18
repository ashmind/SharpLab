using System.Collections.Generic;

namespace SharpLab.Server.Compilation.Internal {
    public interface IFeatureDiscovery {
        IReadOnlyCollection<string> SlowDiscoverAll();
    }
}