using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TryRoslyn.Core.Processing.RoslynSupport;

namespace TryRoslyn.Core.Processing {
    [ThreadSafe]
    public class CodeProcessorProxy : MarshalByRefObject, ICodeProcessor {
        private readonly ICodeProcessor _processor;

        public CodeProcessorProxy() {
            _processor = new LocalCodeProcessor(new Decompiler(), new RoslynAbstraction());
        }

        public ProcessingResult Process(string code, bool scriptMode, bool optimizations) {
            return _processor.Process(code, scriptMode, optimizations);
        }

        public override object InitializeLifetimeService() {
            return null;
        }
        
        public void Dispose() {
            _processor.Dispose();
            // should I force-expire it?
        }
    }
}
