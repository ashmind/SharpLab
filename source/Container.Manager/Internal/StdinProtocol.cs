using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using ProtoBuf;
using SharpLab.Container.Protocol.Stdin;

namespace SharpLab.Container.Manager.Internal {
    public class StdinProtocol {
        public async Task WriteCommandAsync(MultiplexedStream stream, StdinCommand command, CancellationToken cancellationToken) {
            var memoryStream = new MemoryStream();
            Serializer.SerializeWithLengthPrefix(memoryStream, command, PrefixStyle.Base128);
            memoryStream.Seek(0, SeekOrigin.Begin);
            await stream.CopyFromAsync(memoryStream, cancellationToken);
            //await stream.WriteAsync(new byte[1024], 0, 1024, cancellationToken);
        }
    }
}
