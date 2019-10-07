using Autofac;
using JetBrains.Annotations;
using SharpLab.Server.Execution.Internal;
using SharpLab.Server.Execution.Unbreakable;

namespace SharpLab.Server.Execution {
    [UsedImplicitly]
    public class ExecutionModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterInstance(ApiPolicySetup.CreatePolicy())
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
        }
    }
}
