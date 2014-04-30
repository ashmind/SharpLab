using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using TryRoslyn.Core.Decompilation;

namespace TryRoslyn.Core {
    public class CoreModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<Decompiler>().As<IDecompiler>();
            builder.RegisterType<CompilationService>().As<ICompilationService>();
            base.Load(builder);
        }
    }
}