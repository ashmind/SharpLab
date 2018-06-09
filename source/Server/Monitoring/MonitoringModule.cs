using Autofac;
using JetBrains.Annotations;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Monitoring {
    [UsedImplicitly]
    public class MonitoringModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<DefaultTraceMonitor>()
                   .As<IMonitor>()
                   .SingleInstance()
                   .PreserveExistingDefaults();

            builder.RegisterType<MonitorExceptionLogger>()
                   .As<IExceptionLogger>()
                   .SingleInstance();
        }
    }
}