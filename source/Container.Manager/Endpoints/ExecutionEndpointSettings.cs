namespace SharpLab.Container.Manager.Endpoints {
    public class ExecutionEndpointSettings {
        public ExecutionEndpointSettings(string requiredAuthorization) {
            RequiredAuthorization = requiredAuthorization;
        }

        public string RequiredAuthorization { get; }
    }
}
