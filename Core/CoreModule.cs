using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Autofac;
using TryRoslyn.Core.Processing;
using TryRoslyn.Core.Processing.RoslynSupport;

namespace TryRoslyn.Core {
    public class CoreModule : Module {
        protected override void Load(ContainerBuilder builder) {
            var configurationPath = Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            var binariesRoot = Path.Combine(configurationPath, ConfigurationManager.AppSettings["BinariesRoot"]);

            builder.Register(c => new BranchProvider(new DirectoryInfo(binariesRoot)))
                   .As<IBranchProvider>()
                   .SingleInstance();

            builder.RegisterType<RoslynAbstraction>().As<IRoslynAbstraction>().SingleInstance();
            builder.RegisterType<LocalCodeProcessor>().As<ICodeProcessor>().SingleInstance();
            builder.RegisterType<BranchCodeProcessor>().AsSelf().InstancePerDependency();
            builder.Register<ICodeProcessorManager>(c => new CodeProcessorManager(
                c.Resolve<ICodeProcessor>(),
                c.Resolve<Func<string, BranchCodeProcessor>>()
            ));

            builder.RegisterType<Decompiler>().As<IDecompiler>();
        }
    }
}