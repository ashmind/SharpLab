using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using static Docker.DotNet.MultiplexedStream;

namespace SharpLab.Container.Manager.Internal {
    public class StdoutReader {
        private static readonly byte[] ExecutionTimedOut = Encoding.UTF8.GetBytes("\n(Execution timed out)");
        private static readonly byte[] StartOfOutputNotFound = Encoding.UTF8.GetBytes("\n(Could not find start of output)");
        private static readonly byte[] UnexpectedEndOfOutput = Encoding.UTF8.GetBytes("\n(Unexpected end of output)");

        public async Task<ExecutionOutputResult> ReadOutputAsync(
            MultiplexedStream stream,
            byte[] outputBuffer,
            ReadOnlyMemory<byte> outputStartMarker,
            ReadOnlyMemory<byte> outputEndMarker,
            CancellationToken cancellationToken
        ) {
            var currentIndex = 0;
            var outputStartIndex = -1;
            var outputEndIndex = -1;
            var nextStartMarkerIndexToCompare = 0;
            var nextEndMarkerIndexToCompare = 0;
            var cancelled = false;

            while (outputEndIndex < 0) {
                var (read, readCancelled) = await ReadWithCancellationAsync(stream, outputBuffer, currentIndex, outputBuffer.Length - currentIndex, cancellationToken);
                if (readCancelled) {
                    cancelled = true;
                    break;
                }

                if (read.EOF) {
                    await Task.Delay(10, cancellationToken);
                    continue;
                }

                if (outputStartIndex == -1) {
                    var startMarkerEndIndex = GetMarkerEndIndex(
                        outputBuffer, currentIndex, read.Count,
                        outputStartMarker, ref nextStartMarkerIndexToCompare
                    );
                    if (startMarkerEndIndex != -1)
                        outputStartIndex = startMarkerEndIndex;
                }

                // cannot be else if -- it might have changed inside previous if
                if (outputStartIndex != -1) {
                    var endMarkerEndIndex = GetMarkerEndIndex(
                        outputBuffer, currentIndex, read.Count,
                        outputEndMarker, ref nextEndMarkerIndexToCompare
                    );
                    if (endMarkerEndIndex != -1)
                        outputEndIndex = endMarkerEndIndex - outputEndMarker.Length;
                }

                currentIndex += read.Count;
                if (currentIndex >= outputBuffer.Length)
                    break;
            }

            if (outputStartIndex < 0)
                return new(outputBuffer.AsMemory(0, currentIndex), StartOfOutputNotFound);
            if (cancelled)
                return new(outputBuffer.AsMemory(outputStartIndex, currentIndex - outputStartIndex), ExecutionTimedOut);
            if (outputEndIndex < 0)
                return new(outputBuffer.AsMemory(outputStartIndex, currentIndex - outputStartIndex), UnexpectedEndOfOutput);

            return new(outputBuffer.AsMemory(outputStartIndex, outputEndIndex - outputStartIndex));
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

        private static int GetMarkerEndIndex(byte[] outputBuffer, int currentIndex, int length, ReadOnlyMemory<byte> marker, ref int nextMarkerIndexToCompare) {
            var markerEndIndex = -1;
            var searchEndIndex = currentIndex + length;
            for (var i = currentIndex; i < searchEndIndex; i++) {
                if (outputBuffer[i] != marker.Span[nextMarkerIndexToCompare]) {
                    nextMarkerIndexToCompare = outputBuffer[i] == marker.Span[0] ? 1 : 0;
                    continue;
                }

                nextMarkerIndexToCompare += 1;
                if (nextMarkerIndexToCompare == marker.Length) {
                    markerEndIndex = i + 1;
                    break;
                }
            }
            return markerEndIndex;
        }
    }
}
