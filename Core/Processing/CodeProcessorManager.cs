using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AshMind.Extensions;
using JetBrains.Annotations;

namespace TryRoslyn.Core.Processing {
    [ThreadSafe]
    public class CodeProcessorManager : ICodeProcessorManager, IDisposable {
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
        
        public void Dispose() {
            var exceptions = new List<Exception>();
            try {
                DefaultProcessor.Dispose();
            }
            catch (Exception ex) {
                exceptions.Add(ex);
            }

            _branchProcessors.ForEach(p => {
                try {
                    p.Value.Dispose();
                }
                catch (Exception ex) {
                    exceptions.Add(ex);
                }
            });

            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }
    }
}