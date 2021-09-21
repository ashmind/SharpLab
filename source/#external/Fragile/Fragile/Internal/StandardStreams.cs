using System;
using System.IO;
using System.IO.Pipes;

namespace Fragile.Internal {
    internal readonly struct StandardStreams : IDisposable {
        public AnonymousPipeServerStream Input { get; }
        public AnonymousPipeServerStream Output { get; }
        public AnonymousPipeServerStream Error { get; }

        private StandardStreams(
            AnonymousPipeServerStream input,
            AnonymousPipeServerStream output,
            AnonymousPipeServerStream error
        ) : this() {
            Input = input;
            Output = output;
            Error = error;
        }

        public static StandardStreams CreateFromPipes() {
            var input = (AnonymousPipeServerStream?)null;
            var output = (AnonymousPipeServerStream?)null;
            var error = (AnonymousPipeServerStream?)null;

            try {
                input = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
                output = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
                error = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

                return new StandardStreams(input, output, error);
            }
            catch (Exception ex) {
                SafeDispose.DisposeOnException(
                    ex,
                    input, DisposeStream,
                    output, DisposeStream,
                    error, DisposeStream
                );
                throw;
            }
        }

        private static void DisposeStream(Stream? stream) => stream?.Dispose();

        public void Dispose() {
            SafeDispose.Dispose(
                Input, DisposeStream,
                Output, DisposeStream,
                Error, DisposeStream
            );
        }
    }
}
