using System;
using System.Collections.Concurrent;

namespace TryRoslyn.Core.Processing {
    public class CodeProcessorManager : ICodeProcessorManager {
        private readonly Func<string, ICodeProcessor> _getBranchProcessor;
        private readonly ConcurrentDictionary<string, ICodeProcessor> _branchProcessors;

        public CodeProcessorManager(ICodeProcessor defaultProcessor, Func<string, ICodeProcessor> getBranchProcessor) {
            _getBranchProcessor = getBranchProcessor;
            _branchProcessors = new ConcurrentDictionary<string, ICodeProcessor>();
            DefaultProcessor = defaultProcessor;
        }

        public ICodeProcessor DefaultProcessor { get; private set; }

        public ICodeProcessor GetBranchProcessor(string branchName) {
            return _branchProcessors.GetOrAdd(branchName, _getBranchProcessor);
        }
    }
}