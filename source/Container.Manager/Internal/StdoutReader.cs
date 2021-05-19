using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;

namespace SharpLab.Container.Manager.Internal {
    public class StdoutReader {
        public async Task<ReadOnlyMemory<char>> ReadOutputAsync(MultiplexedStream stream, string outputEndMarker, CancellationToken cancellationToken) {
            var byteBuffer = new byte[1024];
            var charBuffer = new char[1024];

            var decoder = Encoding.UTF8.GetDecoder();
            var byteIndex = 0;
            var charIndex = 0;
            var outputEndIndex = -1;
            while (outputEndIndex < 0) {
                var read = await stream.ReadOutputAsync(byteBuffer, byteIndex, byteBuffer.Length - byteIndex, cancellationToken);
                if (read.EOF)
                    break;

                var charCount = decoder.GetChars(byteBuffer, byteIndex, read.Count, charBuffer, charIndex);
                var earliestOutputEndCheckIndex = Math.Min(charIndex, charBuffer.Length - outputEndMarker.Length);
                var relativeOutputEndIndex = ((ReadOnlySpan<char>)charBuffer.AsSpan())
                    .Slice(earliestOutputEndCheckIndex)
                    .IndexOf(outputEndMarker, StringComparison.Ordinal);
                if (relativeOutputEndIndex >= 0)
                    outputEndIndex = earliestOutputEndCheckIndex + relativeOutputEndIndex;

                byteIndex += read.Count;
                charIndex += charCount;
                if (byteIndex >= byteBuffer.Length)
                    break;                
            }
            if (outputEndIndex < 0)
                return ("Garbled?\r\n" + new string(charBuffer, 0, charIndex)).AsMemory();

            return charBuffer.AsMemory(0, outputEndIndex);
        }
    }
}
