using ProtoBuf;

namespace SharpLab.Container.Protocol.Stdin {
    [ProtoContract]
    [ProtoInclude(10, typeof(ExecuteCommand))]
    public abstract class StdinCommand {
    }
}
