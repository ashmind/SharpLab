using System;
using System.Collections.Generic;
using SharpLab.Runtime.Internal;

namespace SharpLab.Server.Execution {
    [Serializable]
    public class ExecutionResult {
        public ExecutionResult(string returnValue, IReadOnlyDictionary<int, Flow.Line> lines) {
            ReturnValue = returnValue;
            Lines = lines;
        }

        public ExecutionResult(Exception exception, IReadOnlyDictionary<int, Flow.Line> lines) {
            Exception = exception;
            Lines = lines;
        }

        public string ReturnValue { get; }
        public Exception Exception { get; }
        public IReadOnlyDictionary<int, Flow.Line> Lines { get; }
    }
}
