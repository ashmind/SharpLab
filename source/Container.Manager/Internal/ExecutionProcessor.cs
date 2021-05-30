using System;
using System.Threading;
using System.Threading.Tasks;
using SharpLab.Container.Protocol.Stdin;

namespace SharpLab.Container.Manager.Internal {
    public class ExecutionProcessor {
        private readonly StdinWriter _stdinWriter;
        private readonly StdoutReader _stdoutReader;

        public ExecutionProcessor(
            StdinWriter stdinWriter,
            StdoutReader stdoutReader
        ) {
            _stdinWriter = stdinWriter;
            _stdoutReader = stdoutReader;
        }

        public async Task<(ReadOnlyMemory<char> output, bool outputFailed)> ExecuteInContainerAsync(ActiveContainer container, byte[] assemblyBytes, CancellationToken cancellationToken) {
            var outputEndMarker = "---END-OUTPUT-" + Guid.NewGuid().ToString();

            await _stdinWriter.WriteCommandAsync(container.Stream, new ExecuteCommand(assemblyBytes, outputEndMarker), cancellationToken);

            using var executeCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            executeCancellationSource.CancelAfter(5000);
            return await _stdoutReader.ReadOutputAsync(container.Stream, outputEndMarker, executeCancellationSource.Token);
        }
    }
}
