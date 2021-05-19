using ProtoBuf;

namespace SharpLab.Container.Protocol.Stdin {
    [ProtoContract]
    [ProtoInclude(10, typeof(ExecuteCommand))]
    [ProtoInclude(11, typeof(ExitCommand))]
    public abstract class StdinCommand {
    }
}
