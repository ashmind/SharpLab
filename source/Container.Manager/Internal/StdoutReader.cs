using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using static Docker.DotNet.MultiplexedStream;

namespace SharpLab.Container.Manager.Internal {
    public class StdoutReader {
        private static readonly byte[] ExecutionTimedOut = Encoding.UTF8.GetBytes("\n(Execution timed out)");
        private static readonly byte[] UnexpectedEndOfOutput = Encoding.UTF8.GetBytes("\n(Unexpected end of output)");

        public async Task<OutputResult> ReadOutputAsync(
            MultiplexedStream stream,
            ReadOnlyMemory<byte> outputEndMarker,
            byte[] outputBytes,
            CancellationToken cancellationToken
        ) {
            var byteIndex = 0;
            var outputEndIndex = -1;
            var cancelled = false;
            var nextEndMarkerIndexToCompare = 0;

            while (outputEndIndex < 0) {
                var (read, readCancelled) = await ReadWithCancellationAsync(stream, outputBytes, byteIndex, outputBytes.Length - byteIndex, cancellationToken);
                if (readCancelled) {
                    cancelled = true;
                    break;
                }

                if (read.EOF)
                    break;

                var totalReadCount = byteIndex + read.Count;
                for (var i = byteIndex; i < totalReadCount; i++) {
                    if (outputBytes[i] != outputEndMarker.Span[nextEndMarkerIndexToCompare]) {
                        nextEndMarkerIndexToCompare = outputBytes[i] == outputEndMarker.Span[0] ? 1 : 0;
                        continue;
                    }

                    nextEndMarkerIndexToCompare += 1;
                    if (nextEndMarkerIndexToCompare >= outputEndMarker.Length) {
                        outputEndIndex = i - outputEndMarker.Length;
                        break;
                    }
                }

                byteIndex += read.Count;
                if (byteIndex >= outputBytes.Length)
                    break;
            }

            if (cancelled)
                return new(outputBytes.AsMemory(0, byteIndex), ExecutionTimedOut);
            if (outputEndIndex < 0)
                return new(outputBytes.AsMemory(0, byteIndex), UnexpectedEndOfOutput);

            return new(outputBytes.AsMemory(0, outputEndIndex));
        }

        // Underlying stream does not handle cancellation correctly by default, see
        // https://stackoverflow.com/questions/12421989/networkstream-readasync-with-a-cancellation-token-never-cancels
        private async Task<(ReadResult result, bool cancelled)> ReadWithCancellationAsync(MultiplexedStream stream, byte[] buffer, int index, int count, CancellationToken cancellationToken) {
            var cancellationTaskSource = new TaskCompletionSource<object?>();
            using var _ = cancellationToken.Register(() => cancellationTaskSource.SetResult(null));

            var result = await Task.WhenAny(
                stream.ReadOutputAsync(buffer, index, count, cancellationToken),
                cancellationTaskSource.Task
            );
            if (result == cancellationTaskSource.Task)
                return (default, true);

            return (await (Task<ReadResult>)result, false);
        }
    }
}
