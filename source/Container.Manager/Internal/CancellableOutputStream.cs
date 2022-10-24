using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLab.Container.Manager.Internal {
    public class CancellableOutputStream : Stream {
        private readonly PipeStream _baseStream;
        private bool _cancellationFailed = false;

        public CancellableOutputStream(PipeStream baseStream) {
            Argument.NotNull(nameof(baseStream), baseStream);
            if (!baseStream.CanRead)
                throw new ArgumentException("Stream must be readable.", nameof(baseStream));
            _baseStream = baseStream;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            if (_cancellationFailed)
                throw new InvalidOperationException("Previous stream read cancellation failed, stream is no longer usable.");

            cancellationToken.ThrowIfCancellationRequested();
            using var cancellationRegistration = cancellationToken.Register(static thisStreamAsObject => {
                var thisStream = (CancellableOutputStream)thisStreamAsObject!;
                if (!NativeMethods.CancelIoEx(thisStream._baseStream.SafePipeHandle, IntPtr.Zero))
                    thisStream._cancellationFailed = true;
            }, this);
            return await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _baseStream.Length;

        public override long Position {
            get => _baseStream.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException();
        }

        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException();
        }
    }
}
