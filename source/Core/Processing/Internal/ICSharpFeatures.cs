using System.Collections.Generic;

namespace TryRoslyn.Core.Processing.Internal {
    public interface ICSharpFeatures {
        IReadOnlyCollection<string> DiscoverAll();
    }
}