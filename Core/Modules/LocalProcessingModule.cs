using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using TryRoslyn.Core.Processing;
using TryRoslyn.Core.Processing.Decompilation;
using TryRoslyn.Core.Processing.RoslynSupport;

namespace TryRoslyn.Core.Modules {
    public class LocalProcessingModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<RoslynAbstraction>().As<IRoslynAbstraction>().SingleInstance();
            builder.RegisterType<CSharpLanguage>().As<IRoslynLanguage>().SingleInstance();
            builder.RegisterType<VBNetLanguage>().As<IRoslynLanguage>().SingleInstance();
            builder.RegisterType<CSharpDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<VBNetDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<LocalCodeProcessor>().As<ICodeProcessor>().SingleInstance();
        }
    }
}