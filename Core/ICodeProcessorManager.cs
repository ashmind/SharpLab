using System;
using System.Collections.Generic;
using System.Linq;

namespace TryRoslyn.Core {
    public interface ICodeProcessorManager {
        ICodeProcessor DefaultProcessor { get; }
        ICodeProcessor GetBranchProcessor(string branchName);
    }
}
