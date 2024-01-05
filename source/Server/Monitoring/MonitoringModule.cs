using Autofac;
using JetBrains.Annotations;

namespace SharpLab.Server.Monitoring;
[UsedImplicitly]
public class MonitoringModule : Module {
    protected override void Load(ContainerBuilder builder) {
        builder.RegisterType<DefaultLoggerExceptionMonitor>()
               .As<IExceptionMonitor>()
               .SingleInstance()
               .PreserveExistingDefaults();

        builder.RegisterType<DefaultLoggerMetricMonitor>()
               .AsSelf()
               .InstancePerDependency();

        builder.Register<MetricMonitorFactory>(c => {
                   var context = c.Resolve<IComponentContext>();
                   return (@namespace, name) => context.Resolve<DefaultLoggerMetricMonitor>(
                       new NamedParameter("namespace", @namespace),
                       new NamedParameter("name", name)
                   );
               })
               .SingleInstance()
               .PreserveExistingDefaults();
    }
}