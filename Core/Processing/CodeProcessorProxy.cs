using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using JetBrains.Annotations;
using TryRoslyn.Core.Modules;

namespace TryRoslyn.Core.Processing {
    [ThreadSafe]
    public class CodeProcessorProxy : MarshalByRefObject, ICodeProcessor {
        private readonly ICodeProcessor _processor;

        public CodeProcessorProxy() {
            var builder = new ContainerBuilder();
            builder.RegisterModule<LocalProcessingModule>();
            var container = builder.Build();

            _processor = container.Resolve<ICodeProcessor>();
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
