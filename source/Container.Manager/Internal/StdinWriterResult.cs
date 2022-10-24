using System;

namespace SharpLab.Container.Manager.Internal {
    public readonly struct StdinWriterResult {
        private StdinWriterResult(bool isSuccess, ReadOnlyMemory<byte> failureMessage) {
            IsSuccess = isSuccess;
            FailureMessage = failureMessage;
        }

        public static StdinWriterResult Success { get; } = new (true, ReadOnlyMemory<byte>.Empty);
        public static StdinWriterResult Failure(ReadOnlyMemory<byte> message) => new (false, message);

        public bool IsSuccess { get; }
        public ReadOnlyMemory<byte> FailureMessage { get; }        
    }
}
