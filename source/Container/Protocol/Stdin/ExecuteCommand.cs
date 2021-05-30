using System;
using ProtoBuf;

namespace SharpLab.Container.Protocol.Stdin {
    [ProtoContract(SkipConstructor = true)]
    public class ExecuteCommand : StdinCommand {
        public ExecuteCommand(byte[] assemblyBytes, Guid outputEndMarker) {
            AssemblyBytes = assemblyBytes;
            OutputEndMarker = outputEndMarker;
        }

        [ProtoMember(1, Options = MemberSerializationOptions.OverwriteList | MemberSerializationOptions.Packed)]
        public byte[] AssemblyBytes { get; }
        [ProtoMember(2)]
        public Guid OutputEndMarker { get; }
    }
}
