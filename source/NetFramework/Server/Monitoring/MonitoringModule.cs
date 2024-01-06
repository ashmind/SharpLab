using Autofac;
using JetBrains.Annotations;
using System;

namespace SharpLab.Server.Monitoring;
[UsedImplicitly]
public class MonitoringModule : Module {
    protected override void Load(ContainerBuilder builder) {
        builder.RegisterType<DefaultTraceMetricMonitor>()
               .AsSelf()
               .InstancePerDependency();

        builder.RegisterType<DefaultTraceMonitor>()
               .As<IMonitor>()
               .WithParameter(
                   (p, _) => p.ParameterType == typeof(Func<(string, string), DefaultTraceMetricMonitor>),
                   (_, c) => {
                       var context = c.Resolve<IComponentContext>();
                       return ((string @namespace, string name) args) => context.Resolve<DefaultTraceMetricMonitor>(
                           new NamedParameter("namespace", args.@namespace),
                           new NamedParameter("name", args.name)
                       );
                   }
               )
               .SingleInstance()
               .PreserveExistingDefaults();
    }
}