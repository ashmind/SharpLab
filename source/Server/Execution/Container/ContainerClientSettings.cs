using System;

namespace SharpLab.Server.Execution.Container {
    public class ContainerClientSettings {
        public ContainerClientSettings(Uri runnerUrl) {
            RunnerUrl = runnerUrl;
        }

        public Uri RunnerUrl { get; }
    }
}
