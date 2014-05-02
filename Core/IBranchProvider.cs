using System.Collections.Immutable;
using System.IO;
using JetBrains.Annotations;

namespace TryRoslyn.Core {
    public interface IBranchProvider {
        [NotNull] IImmutableList<BranchInfo> GetBranches();
        [NotNull] DirectoryInfo GetDirectory([NotNull] string branchName);
    }
}