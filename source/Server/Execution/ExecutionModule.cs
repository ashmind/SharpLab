using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Runtime;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common;
using SharpLab.Server.Execution.Container;
using SharpLab.Server.Execution.Internal;
using SharpLab.Server.Execution.Runtime;
using SharpLab.Server.Execution.Unbreakable;

namespace SharpLab.Server.Execution {
    [UsedImplicitly]
    public class ExecutionModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterInstance(ApiPolicySetup.CreatePolicy())
                   .AsSelf()
                   .SingleInstance();

            builder.Register(_ => {
                var dataTarget = DataTarget.AttachToProcess(Current.ProcessId, uint.MaxValue, AttachFlag.Passive);
                return dataTarget.ClrVersions.Single(c => c.Flavor == ClrFlavor.Core).CreateRuntime();
            }).SingleInstance();

            builder.RegisterType<Pool<ClrRuntime>>()
                   .AsSelf()
                   .SingleInstance();

            builder.RegisterType<ExecutionResultSerializer>()
                   .AsSelf()
                   .SingleInstance();

            builder.RegisterType<FlowReportingRewriter>()
                   .As<IAssemblyRewriter>()
                   .SingleInstance();

            builder.RegisterType<MemoryGraphArgumentNamesRewriter>()
                   .As<IAssemblyRewriter>()
                   .SingleInstance();

            builder.RegisterType<FSharpEntryPointRewriter>()
                   .As<IAssemblyRewriter>()
                   .SingleInstance();

            builder.RegisterType<Executor>()
                   .As<IExecutor>()
                   .SingleInstance();

            var containerHostUrl = Environment.GetEnvironmentVariable("SHARPLAB_CONTAINER_HOST_URL")
                ?? throw new Exception("Required environment variable SHARPLAB_CONTAINER_HOST_URL was not provided.");
            var containerAuthorizationToken = Environment.GetEnvironmentVariable("SHARPLAB_CONTAINER_HOST_ACCESS_TOKEN")
                ?? throw new Exception("Required environment variable SHARPLAB_CONTAINER_HOST_URL was not provided.");

            builder.RegisterType<ContainerFlowReportingRewriter>()
                   .As<IContainerAssemblyRewriter>()
                   .SingleInstance();

            builder.RegisterInstance(new ContainerClientSettings(new Uri(containerHostUrl), containerAuthorizationToken))
                   .AsSelf();

            builder.RegisterType<ContainerClient>()
                   .AsSelf()
                   .SingleInstance();

            builder.RegisterType<ContainerExecutor>()
                   .As<IContainerExecutor>()
                   .SingleInstance();

            RegisterRuntime(builder);
        }

        private void RegisterRuntime(ContainerBuilder builder) {
            builder.RegisterType<ValuePresenter>()
                   .As<IValuePresenter>()
                   .SingleInstance();

            builder.RegisterType<MemoryBytesInspector>()
                   .As<IMemoryBytesInspector>()
                   .SingleInstance();

            builder.RegisterType<MemoryGraphBuilder>()
                   .As<IMemoryGraphBuilder>()
                   .InstancePerDependency();

            builder.RegisterType<AllocationInspector>()
                   .As<IAllocationInspector>()
                   .SingleInstance();

            builder.RegisterBuildCallback(c => {
                RuntimeServices.ValuePresenter = c.Resolve<IValuePresenter>();
                RuntimeServices.MemoryBytesInspector = c.Resolve<IMemoryBytesInspector>();
                RuntimeServices.MemoryGraphBuilderFactory = c.Resolve<Func<IReadOnlyList<string>, IMemoryGraphBuilder>>();
                RuntimeServices.AllocationInspector = c.Resolve<IAllocationInspector>();
            });
        }
    }
}