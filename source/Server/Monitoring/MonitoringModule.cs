using Autofac;
using JetBrains.Annotations;

namespace SharpLab.Server.Monitoring {
    [UsedImplicitly]
    public class MonitoringModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<DefaultTraceMonitor>()
                   .As<IMonitor>()
                   .SingleInstance()
                   .PreserveExistingDefaults();
        }
    }
}