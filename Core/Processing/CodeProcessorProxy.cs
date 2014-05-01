using System;
using System.Collections.Generic;
using System.Linq;
using TryRoslyn.Core.Processing.RoslynSupport;

namespace TryRoslyn.Core.Processing {
    public class CodeProcessorProxy : MarshalByRefObject, ICodeProcessor {
        private readonly ICodeProcessor _processor;

        public CodeProcessorProxy() {
            _processor = new LocalCodeProcessor(new Decompiler(), new RoslynAbstraction());
        }

        public ProcessingResult Process(string code) {
            return _processor.Process(code);
        }
    }
}
