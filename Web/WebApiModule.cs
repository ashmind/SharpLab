using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using TryRoslyn.Web.Internal;

namespace TryRoslyn.Web {
    public class WebApiModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<Decompiler>().As<IDecompiler>();
            builder.RegisterType<CompilationService>().As<ICompilationService>();
            base.Load(builder);
        }
    }
}