using System;

namespace SharpLab.Server.Execution.Container {
    public class ContainerClientSettings {
        public ContainerClientSettings(Uri runnerUrl, string authorizationToken) {
            RunnerUrl = runnerUrl;
            AuthorizationToken = authorizationToken;
        }

        public Uri RunnerUrl { get; }
        public string AuthorizationToken { get; }
    }
}
