using System;
using System.Buffers;
using System.Buffers.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpLab.Container.Protocol.Stdin;

namespace SharpLab.Container.Manager.Internal {
    public class ExecutionProcessor {
        private readonly StdinWriter _stdinWriter;
        private readonly StdoutReader _stdoutReader;

        public ExecutionProcessor(StdinWriter stdinWriter, StdoutReader stdoutReader) {
            _stdinWriter = stdinWriter;
            _stdoutReader = stdoutReader;
        }

        public async Task<ExecutionOutputResult> ExecuteInContainerAsync(
            ActiveContainer container,            
            byte[] assemblyBytes,
            byte[] outputBufferBytes,
            bool includePerformance,
            CancellationToken cancellationToken
        ) {
            var outputEndMarker = Guid.NewGuid();
            await _stdinWriter.WriteCommandAsync(
                container.Stream, new ExecuteCommand(assemblyBytes, outputEndMarker, includePerformance), cancellationToken
            );

            const int OutputEndMarkerLength = 36; // length of guid
            var outputEndMarkerBytes = ArrayPool<byte>.Shared.Rent(OutputEndMarkerLength);
            try {
                Utf8Formatter.TryFormat(outputEndMarker, outputEndMarkerBytes, out _);
                using var executionCancellation = CancellationFactory.ContainerExecution(cancellationToken);
                return await _stdoutReader.ReadOutputAsync(
                    container.Stream,
                    outputEndMarkerBytes.AsMemory(0, OutputEndMarkerLength),
                    outputBufferBytes,
                    executionCancellation.Token
                );
            }
            finally {
                ArrayPool<byte>.Shared.Return(outputEndMarkerBytes);
            }
        }
    }
}
