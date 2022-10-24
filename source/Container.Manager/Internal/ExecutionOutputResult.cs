using System;

namespace SharpLab.Container.Manager.Internal {
    public readonly struct ExecutionOutputResult {
        private static class Messages {
        }

        private ExecutionOutputResult(ReadOnlyMemory<byte> output, ReadOnlyMemory<byte> failureMessage) {
            Output = output;
            FailureMessage = failureMessage;
        }

        public static ExecutionOutputResult Success(ReadOnlyMemory<byte> output) => new(output, default);
        public static ExecutionOutputResult Failure(ReadOnlyMemory<byte> failureMessage, ReadOnlyMemory<byte> output = default) => new(output, failureMessage);

        public ReadOnlyMemory<byte> Output { get; }
        public ReadOnlyMemory<byte> FailureMessage { get; }
        public bool IsSuccess => FailureMessage.IsEmpty;
    }
}
