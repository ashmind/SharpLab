using System;

namespace SharpLab.Container.Manager.Internal {
    public readonly struct OutputResult {
        public OutputResult(ReadOnlyMemory<byte> output) {
            Output = output;
            FailureMessage = default;
        }

        public OutputResult(ReadOnlyMemory<byte> output, ReadOnlyMemory<byte> failureMessage) {
            Output = output;
            FailureMessage = failureMessage;
        }

        public ReadOnlyMemory<byte> Output { get; }
        public ReadOnlyMemory<byte> FailureMessage { get; }
        public bool IsSuccess => FailureMessage.IsEmpty;
    }
}
