using Docker.DotNet;

namespace SharpLab.Container.Manager.Internal {
    public class SessionContainer {
        public SessionContainer(
            string sessionId,
            DockerClient client,
            string containerId,
            MultiplexedStream stream
        ) {
            SessionId = sessionId;
            Client = client;
            ContainerId = containerId;
            Stream = stream;
        }

        public string SessionId { get; }
        public DockerClient Client { get; }
        public string ContainerId { get; }
        public MultiplexedStream Stream { get; }
    }
}
