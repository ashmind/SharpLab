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
            var abstraction = new RoslynAbstraction();
            _processor = new LocalCodeProcessor(
                new Decompiler(), abstraction,
                new CSharpLanguage(abstraction),
                new VBNetLanguage(abstraction)
            );
        }

        public ProcessingResult Process(string code, ProcessingOptions options) {
            return _processor.Process(code, options);
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
