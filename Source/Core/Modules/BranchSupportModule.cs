using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using AshMind.IO.Abstractions;
using AshMind.IO.Abstractions.Adapters;
using Autofac;
using TryRoslyn.Core.Processing;

namespace TryRoslyn.Core.Modules {
    public class BranchSupportModule : Module {
        protected override void Load(ContainerBuilder builder) {
            var configurationPath = Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            var binariesRoot = Path.Combine(configurationPath, ConfigurationManager.AppSettings["BinariesRoot"]);

            builder.RegisterType<FileSystem>()
                   .As<IFileSystem>()
                   .SingleInstance();

            builder.Register(c => new BranchProvider(new DirectoryInfoAdapter(new DirectoryInfo(binariesRoot))))
                   .As<IBranchProvider>()
                   .SingleInstance();

            builder.RegisterType<CodeProcessor>()
                   .As<ICodeProcessor>()
                   .SingleInstance();
        }
    }
}