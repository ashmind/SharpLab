using System;

namespace SharpLab.Container.Manager.Internal {
    public readonly struct ExecutionOutputResult {
        public ExecutionOutputResult(ReadOnlyMemory<byte> output) {
            Output = output;
            OutputReadFailureMessage = default;
        }

        public ExecutionOutputResult(ReadOnlyMemory<byte> output, ReadOnlyMemory<byte> outputReadFailureMessage) {
            Output = output;
            OutputReadFailureMessage = outputReadFailureMessage;
        }

        public ReadOnlyMemory<byte> Output { get; }
        public ReadOnlyMemory<byte> OutputReadFailureMessage { get; }
        public bool IsOutputReadSuccess => OutputReadFailureMessage.IsEmpty;
    }
}
