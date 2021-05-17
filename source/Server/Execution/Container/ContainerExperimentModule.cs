using System;
using Autofac;
using JetBrains.Annotations;

namespace SharpLab.Server.Execution.Container {
    [UsedImplicitly]
    public class ContainerExperimentModule : Module {
        protected override void Load(ContainerBuilder builder) {
            var accessKey = Environment.GetEnvironmentVariable("SHARPLAB_CONTAINER_EXPERIMENT_KEY");
            if (accessKey == null)
                throw new Exception("Required key SHARPLAB_CONTAINER_EXPERIMENT_KEY was not provided.");

            var settings = new ContainerExperimentSettings(accessKey);
            builder.RegisterInstance(settings).AsSelf();
        }
    }
}
