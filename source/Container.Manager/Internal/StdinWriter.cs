using System.IO;
using ProtoBuf;
using SharpLab.Container.Protocol.Stdin;

namespace SharpLab.Container.Manager.Internal {
    public class StdinWriter {
        public void WriteCommand(Stream stream, StdinCommand command) {
            Serializer.SerializeWithLengthPrefix(stream, command, PrefixStyle.Base128);
        }
    }
}
