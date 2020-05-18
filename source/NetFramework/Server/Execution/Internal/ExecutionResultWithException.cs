using System;

namespace SharpLab.Server.Execution {
    [Serializable]
    public readonly struct ExecutionResultWithException {
        public ExecutionResultWithException(ExecutionResult result, Exception? exception = null) {
            Result = result;
            Exception = exception;
        }

        public void Deconstruct(out ExecutionResult result, out Exception? exception) {
            result = Result;
            exception = Exception;
        }

        public ExecutionResult Result { get; }
        public Exception? Exception { get; }
    }
}