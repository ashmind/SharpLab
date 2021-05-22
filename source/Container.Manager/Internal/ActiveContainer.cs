using Docker.DotNet;

namespace SharpLab.Container.Manager.Internal {
    public class ActiveContainer {
        public ActiveContainer(
            DockerClient client,
            string containerId,
            MultiplexedStream stream
        ) {
            Client = client;
            ContainerId = containerId;
            Stream = stream;
        }

        public DockerClient Client { get; }
        public string ContainerId { get; }
        public MultiplexedStream Stream { get; }
    }
}
