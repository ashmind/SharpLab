using System;
using System.Collections.Generic;
using SharpLab.Runtime.Internal;

namespace SharpLab.Server.Execution {
    [Serializable]
    public class ExecutionResult {
        public ExecutionResult(IReadOnlyList<object> output, IReadOnlyList<Flow.Step> flow) {
            Output = output;
            Flow = flow;
        }

        public IReadOnlyList<object> Output { get; }
        public IReadOnlyList<Flow.Step> Flow { get; }
    }
}
