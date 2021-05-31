using Docker.DotNet;

namespace SharpLab.Container.Manager.Internal {
    public class ActiveContainer {
        public ActiveContainer(string containerId, MultiplexedStream stream) {
            ContainerId = containerId;
            Stream = stream;
        }

        public string ContainerId { get; }
        public MultiplexedStream Stream { get; }
    }
}
