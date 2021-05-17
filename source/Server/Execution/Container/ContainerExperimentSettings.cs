namespace SharpLab.Server.Execution.Container {
    public class ContainerExperimentSettings {
        public ContainerExperimentSettings(string accessKey) {
            AccessKey = accessKey;
        }

        public string AccessKey { get; }
    }
}
