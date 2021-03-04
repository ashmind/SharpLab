using Autofac;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using MirrorSharp.Advanced;
using MirrorSharp.Advanced.EarlyAccess;
using SharpLab.Server.MirrorSharp.Guards;

namespace SharpLab.Server.MirrorSharp {
    [UsedImplicitly]
    public class MirrorSharpModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<SlowUpdate>()
                   .As<ISlowUpdateExtension>()
                   .SingleInstance();

            builder.RegisterType<SetOptionsFromClient>()
                   .As<ISetOptionsFromClientExtension>()
                   .SingleInstance();

            builder.RegisterType<CSharpCompilationGuard>()
                   .As<IRoslynCompilationGuard<CSharpCompilation>>()
                   .SingleInstance();

            builder.RegisterType<VisualBasicCompilationGuard>()
                   .As<IRoslynCompilationGuard<VisualBasicCompilation>>()
                   .SingleInstance();

            builder.RegisterType<RoslynCompilationGuard>()
                   .As<IRoslynCompilationGuard>()
                   .SingleInstance();

            builder.RegisterType<RoslynSourceTextGuard>()
                   .As<IRoslynSourceTextGuard>()
                   .SingleInstance();
        }
    }
}