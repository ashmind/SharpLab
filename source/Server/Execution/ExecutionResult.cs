using System;
using System.Collections.Generic;
using SharpLab.Runtime.Internal;

namespace SharpLab.Server.Execution {
    [Serializable]
    public class ExecutionResult {
        public ExecutionResult(string returnValue, IReadOnlyList<Flow.Step> flow) {
            ReturnValue = returnValue;
            Flow = flow;
        }

        public ExecutionResult(Exception exception, IReadOnlyList<Flow.Step> flow) {
            Exception = exception;
            Flow = flow;
        }

        public string ReturnValue { get; }
        public Exception Exception { get; }
        public IReadOnlyList<Flow.Step> Flow { get; }
    }
}
