using System;

namespace TryRoslyn.Core.Processing {
    public class CodeProcessorManager : ICodeProcessorManager {
        private readonly Func<string, ICodeProcessor> _getBranchProcessor;

        public CodeProcessorManager(ICodeProcessor defaultProcessor, Func<string, ICodeProcessor> getBranchProcessor) {
            _getBranchProcessor = getBranchProcessor;
            DefaultProcessor = defaultProcessor;
        }

        public ICodeProcessor DefaultProcessor { get; private set; }

        public ICodeProcessor GetBranchProcessor(string branchName) {
            return _getBranchProcessor(branchName);
        }
    }
}