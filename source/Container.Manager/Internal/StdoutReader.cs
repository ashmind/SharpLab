using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SharpLab.Container.Manager.Internal {
    public class StdoutReader {
        private static readonly byte[] ExecutionTimedOut = Encoding.UTF8.GetBytes("\n(Execution timed out)");
        private static readonly byte[] StartOfOutputNotFound = Encoding.UTF8.GetBytes("\n(Could not find start of output)");
        private static readonly byte[] UnexpectedEndOfOutput = Encoding.UTF8.GetBytes("\n(Unexpected end of output)");
        private readonly ILogger<StdoutReader> _logger;

        public StdoutReader(ILogger<StdoutReader> logger) {
            _logger = logger;
        }

        public async Task<ExecutionOutputResult> ReadOutputAsync(
            Stream stream,
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
                int readCount;
                try {
                    readCount = await stream.ReadAsync(outputBuffer, currentIndex, outputBuffer.Length - currentIndex, cancellationToken);
                }
                catch (OperationCanceledException) {
                    cancelled = true;
                    _logger.LogDebug("Timeout at stream.ReadAsync");
                    break;
                }

                if (readCount == 0) {
                    try {
                        await Task.Delay(10, cancellationToken);
                    }
                    catch (OperationCanceledException) {
                        cancelled = true;
                        break;
                    }
                    continue;
                }

                if (outputStartIndex == -1) {
                    var startMarkerEndIndex = GetMarkerEndIndex(
                        outputBuffer, currentIndex, readCount,
                        outputStartMarker, ref nextStartMarkerIndexToCompare
                    );
                    if (startMarkerEndIndex != -1)
                        outputStartIndex = startMarkerEndIndex;
                }

                // cannot be else if -- it might have changed inside previous if
                if (outputStartIndex != -1) {
                    var endMarkerEndIndex = GetMarkerEndIndex(
                        outputBuffer, currentIndex, readCount,
                        outputEndMarker, ref nextEndMarkerIndexToCompare
                    );
                    if (endMarkerEndIndex != -1)
                        outputEndIndex = endMarkerEndIndex - outputEndMarker.Length;
                }

                currentIndex += readCount;
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
