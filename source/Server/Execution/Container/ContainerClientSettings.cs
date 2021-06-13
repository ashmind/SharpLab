using System;

namespace SharpLab.Server.Execution.Container {
    public class ContainerClientSettings {
        public ContainerClientSettings(Uri containerHostUrl, string authorizationToken) {
            Argument.NotNull(nameof(containerHostUrl), containerHostUrl);
            Argument.NotNull(nameof(authorizationToken), authorizationToken);

            ContainerHostUrl = containerHostUrl;
            AuthorizationToken = authorizationToken;
        }

        public Uri ContainerHostUrl { get; }
        public string AuthorizationToken { get; }
    }
}
