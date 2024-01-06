using Autofac;
using JetBrains.Annotations;
using System;

namespace SharpLab.Server.Monitoring;
[UsedImplicitly]
public class MonitoringModule : Module {
    protected override void Load(ContainerBuilder builder) {
        builder.RegisterType<DefaultLoggerMetricMonitor>()
               .AsSelf()
               .InstancePerDependency();

        builder.RegisterType<DefaultLoggerMonitor>()
               .As<IMonitor>()
               .WithParameter(
                   (p, _) => p.ParameterType == typeof(Func<(string, string), DefaultLoggerMetricMonitor>),
                   (_, c) => {
                       var context = c.Resolve<IComponentContext>();
                       return ((string @namespace, string name) args) => context.Resolve<DefaultLoggerMetricMonitor>(
                           new NamedParameter("namespace", args.@namespace),
                           new NamedParameter("name", args.name)
                       );
                   }
               )
               .SingleInstance()
               .PreserveExistingDefaults();
    }
}