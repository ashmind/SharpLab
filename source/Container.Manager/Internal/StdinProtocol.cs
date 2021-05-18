using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;

namespace SharpLab.Container.Manager.Internal {
    public class StdinProtocol {
        private static readonly byte[] ExecuteCommand = Encoding.UTF8.GetBytes("EXECUTE:");
        private static readonly byte[] NewLine = Encoding.UTF8.GetBytes("\n");

        public async Task WriteExecuteAsync(MultiplexedStream stream, Stream assemblyStream, CancellationToken cancellationToken) {
            var assemblyStreamLengthBytes = Encoding.UTF8.GetBytes(assemblyStream.Length.ToString());

            await WriteAsync(stream, ExecuteCommand, cancellationToken);
            await WriteAsync(stream, assemblyStreamLengthBytes, cancellationToken);
            await WriteAsync(stream, NewLine, cancellationToken);
            await stream.CopyFromAsync(assemblyStream, cancellationToken);
        }

        private Task WriteAsync(MultiplexedStream stream, byte[] buffer, CancellationToken cancellationToken) {
            return stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }
    }
}
