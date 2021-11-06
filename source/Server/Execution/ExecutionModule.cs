using System;
using System.Linq;
using Autofac;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Runtime;
using SharpLab.Server.Common;
using SharpLab.Server.Execution.Container;
using SharpLab.Server.Execution.Internal;

namespace SharpLab.Server.Execution {
    [UsedImplicitly]
    public class ExecutionModule : Module {
        protected override void Load(ContainerBuilder builder) {
            var containerHostUrl = EnvironmentHelper.GetRequiredEnvironmentVariable("SHARPLAB_CONTAINER_HOST_URL");

            builder.Register(_ => {
                var dataTarget = DataTarget.AttachToProcess(Current.ProcessId, suspend: false);
                return dataTarget.ClrVersions.Single(c => c.Flavor == ClrFlavor.Core).CreateRuntime();
            }).SingleInstance();

            builder.RegisterType<Pool<ClrRuntime>>()
                   .AsSelf()
                   .SingleInstance();

            builder.RegisterType<MemoryGraphArgumentNamesRewriter>()
                   .As<IContainerAssemblyRewriter>()
                   .SingleInstance();

            builder.RegisterType<FSharpEntryPointRewriter>()
                   .As<IContainerAssemblyRewriter>()
                   .SingleInstance();

            builder.RegisterType<ContainerFlowReportingRewriter>()
                   .As<IContainerAssemblyRewriter>()
                   .SingleInstance();

            builder.Register(c => {
                var secretsClient = c.Resolve<ISecretsClient>();
                var containerAuthorizationToken = secretsClient.GetSecret("ContainerHostAuthorizationToken");
                return new ContainerClientSettings(new Uri(containerHostUrl), containerAuthorizationToken);
            }).SingleInstance()
              .AsSelf();

            builder.RegisterType<ContainerClient>()
                   .As<IContainerClient>()
                   .SingleInstance();

            builder.RegisterType<ContainerExecutor>()
                   .As<IContainerExecutor>()
                   .SingleInstance();
        }
    }
}