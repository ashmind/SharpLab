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
            bool isWarmup,
            CancellationToken cancellationToken
        ) {
            var outputStartMarker = Guid.NewGuid();
            var outputEndMarker = Guid.NewGuid();
            _stdinWriter.WriteCommand(container.InputStream, new ExecuteCommand(
                assemblyBytes,
                outputStartMarker,
                outputEndMarker,
                includePerformance
            ));

            const int OutputMarkerLength = 36; // length of guid
            byte[]? outputStartMarkerBytes = null;
            byte[]? outputEndMarkerBytes = null;
            try {
                outputStartMarkerBytes = ArrayPool<byte>.Shared.Rent(OutputMarkerLength);
                outputEndMarkerBytes = ArrayPool<byte>.Shared.Rent(OutputMarkerLength);

                Utf8Formatter.TryFormat(outputStartMarker, outputStartMarkerBytes, out _);
                Utf8Formatter.TryFormat(outputEndMarker, outputEndMarkerBytes, out _);

                using var executionCancellation = isWarmup
                    ? CancellationFactory.ContainerWarmup(cancellationToken)
                    : CancellationFactory.ContainerExecution(cancellationToken);

                return await _stdoutReader.ReadOutputAsync(
                    container.Container.OutputStream,
                    outputBufferBytes,
                    outputStartMarkerBytes.AsMemory(0, OutputMarkerLength),
                    outputEndMarkerBytes.AsMemory(0, OutputMarkerLength),
                    executionCancellation.Token
                );
            }
            finally {
                if (outputStartMarkerBytes != null)
                    ArrayPool<byte>.Shared.Return(outputStartMarkerBytes);
                if (outputEndMarkerBytes != null)
                    ArrayPool<byte>.Shared.Return(outputEndMarkerBytes);
            }
        }
    }
}
