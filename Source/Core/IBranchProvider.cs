using System.Collections.Immutable;
using AshMind.IO.Abstractions;
using JetBrains.Annotations;

namespace TryRoslyn.Core {
    [ThreadSafe]
    public interface IBranchProvider {
        [NotNull] IImmutableList<BranchInfo> GetBranches();
        [NotNull] IDirectory GetDirectory([NotNull] string branchName);
    }
}