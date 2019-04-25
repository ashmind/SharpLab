using Autofac;
using JetBrains.Annotations;
using SharpLab.Server.Execution;

namespace SharpLab.Server.Owin.Execution {
    [UsedImplicitly]
    public class ExecutionModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<Executor>()
                   .As<IExecutor>()
                   .SingleInstance();
        }
    }
}
