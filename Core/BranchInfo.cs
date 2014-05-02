using System;
using System.Collections.Generic;
using System.Linq;

namespace TryRoslyn.Core {
    public class BranchInfo {
        public string Name { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }

        public BranchInfo(string name, DateTimeOffset timestamp) {
            Name = name;
            Timestamp = timestamp;
        }
    }
}
