using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace TryRoslyn.Core {
    [ThreadSafe]
    public interface ICodeProcessorManager {
        ICodeProcessor DefaultProcessor { get; }
        ICodeProcessor GetBranchProcessor(string branchName);
    }
}
