using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AshMind.IO.Abstractions;
using JetBrains.Annotations;

namespace TryRoslyn.Core {
    [ThreadSafe]
    public class BranchProvider : IBranchProvider {
        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly IDirectory _root;

        public BranchProvider([NotNull] IDirectory root) {
            _root = root;
        }

        [Pure]
        public IImmutableList<BranchInfo> GetBranches() {
            return _root.GetDirectories().Select(GetBranch).ToImmutableList();
        }

        [Pure]
        private BranchInfo GetBranch(IDirectory directory) {
            var lastCommitFile = directory.GetFile("LastCommit.txt");
            var lastCommitText = lastCommitFile.ReadAllText();
            var match = Regex.Match(lastCommitText, "(?<hash>[^ ]+) (?<date>[^ ]+) (?<author>[^\r\n]+)[\r\n]+(?<message>.+)", RegexOptions.Singleline);
            var date = DateTime.ParseExact(match.Groups["date"].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            return new BranchInfo(
                directory.Name, date,
                match.Groups["hash"].Value,
                match.Groups["author"].Value,
                match.Groups["message"].Value
            );
        }

        [Pure]
        public IDirectory GetDirectory(string branchName) {
            // ReSharper disable once AssignNullToNotNullAttribute
            return _root.GetDirectory(branchName);
        }
    }
}
