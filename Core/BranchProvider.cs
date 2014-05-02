using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace TryRoslyn.Core {
    public class BranchProvider : IBranchProvider {
        private readonly DirectoryInfo _root;

        public BranchProvider([NotNull] DirectoryInfo root) {
            _root = root;
        }

        public IImmutableList<BranchInfo> GetBranches() {
            return _root.GetDirectories()
                        .Select(d => new BranchInfo(d.Name, d.LastWriteTimeUtc))
                        .ToImmutableList();
        }

        public DirectoryInfo GetDirectory(string branchName) {
            return new DirectoryInfo(Path.Combine(_root.FullName, branchName));
        }
    }
}
