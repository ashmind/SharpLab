using ProtoBuf;

namespace SharpLab.Container.Protocol.Stdin {
    [ProtoContract(SkipConstructor = true)]
    public class ExecuteCommand : StdinCommand {
        public ExecuteCommand(byte[] assemblyBytes, string outputId) {
            AssemblyBytes = assemblyBytes;
            OutputId = outputId;
        }

        [ProtoMember(1, Options = MemberSerializationOptions.OverwriteList | MemberSerializationOptions.Packed)]
        public byte[] AssemblyBytes { get; }

        [ProtoMember(2)]
        public string OutputId { get; }
    }
}
