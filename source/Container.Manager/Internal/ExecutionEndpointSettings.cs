namespace SharpLab.Container.Manager.Internal {
    public class ExecutionEndpointSettings {
        public ExecutionEndpointSettings(string requiredAuthorization) {
            RequiredAuthorization = requiredAuthorization;
        }

        public string RequiredAuthorization { get; }
    }
}
