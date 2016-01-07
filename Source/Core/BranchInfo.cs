using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace TryRoslyn.Core {
    [ReadOnly]
    public class BranchInfo {
        public string Name { get; private set; }
        public DateTimeOffset LastCommitDate { get; private set; }
        public string LastCommitHash { get; private set; }
        public string LastCommitAuthor { get; private set; }
        public string LastCommitMessage { get; private set; }

        public BranchInfo(string name, DateTimeOffset lastCommitDate, string lastCommitHash, string lastCommitAuthor, string lastCommitMessage) {
            Name = name;
            LastCommitDate = lastCommitDate;
            LastCommitHash = lastCommitHash;
            LastCommitAuthor = lastCommitAuthor;
            LastCommitMessage = lastCommitMessage;
        }
    }
}
