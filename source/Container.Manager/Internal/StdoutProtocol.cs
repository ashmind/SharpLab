using System;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;

namespace SharpLab.Container.Manager.Internal {
    public class StdoutProtocol {
        private const string EndOutput = Protocol.StdoutProtocol.EndOutput;

        public async Task<ReadOnlyMemory<char>> ReadOutputAsync(MultiplexedStream stream, string executionId, CancellationToken cancellationToken) {
            var (output, _) = await stream.ReadOutputToEndAsync(cancellationToken);
            var endOutputIndex = output.IndexOf(EndOutput);

            if (!output.AsSpan().Slice(endOutputIndex + EndOutput.Length + 1, executionId.Length).Equals(executionId, StringComparison.Ordinal))
                return ("Garbled?\r\n" + output.Substring(endOutputIndex + EndOutput.Length + 1, executionId.Length) + "\r\n" + executionId + "\r\n" + output).AsMemory();

            return output.AsMemory().Slice(0, endOutputIndex);
        }
    }
}
