namespace SharpLab.Container.Manager.Endpoints {
    public class ExecutionEndpointSettings {
        public ExecutionEndpointSettings(string requiredAuthorizationToken) {
            RequiredAuthorizationToken = requiredAuthorizationToken;
        }

        public string RequiredAuthorizationToken { get; }
    }
}
