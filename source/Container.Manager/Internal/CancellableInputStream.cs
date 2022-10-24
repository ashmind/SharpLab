using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace SharpLab.Container.Manager.Internal {
    public class CancellableInputStream : Stream {
        private readonly PipeStream _baseStream;
        private bool _cancellationFailed = false;

        public CancellableInputStream(PipeStream baseStream) {
            Argument.NotNull(nameof(baseStream), baseStream);
            if (!baseStream.CanWrite)
                throw new ArgumentException("Stream must be writable.", nameof(baseStream));
            _baseStream = baseStream;
        }

        public override void Write(byte[] buffer, int offset, int count) {
            if (_cancellationFailed)
                throw new InvalidOperationException("Previous stream read cancellation failed, stream is no longer usable.");
            if (CancellationToken is not {} cancellationToken)
                throw new InvalidOperationException("CancellationToken must be set before calling Write.");

            cancellationToken.ThrowIfCancellationRequested();
            using var cancellationRegistration = cancellationToken.Register(static thisStreamAsObject => {
                var thisStream = (CancellableInputStream)thisStreamAsObject!;
                if (!NativeMethods.CancelIoEx(thisStream._baseStream.SafePipeHandle, IntPtr.Zero))
                    thisStream._cancellationFailed = true;
            }, this);
            _baseStream.Write(buffer, offset, count);
        }

        public CancellationToken? CancellationToken { get; set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void WriteByte(byte value) => throw new NotSupportedException();
        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
    }
}
