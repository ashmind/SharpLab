using Autofac;
using JetBrains.Annotations;

namespace SharpLab.Server.Compilation {
    [UsedImplicitly]
    public class CompilationModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<Compiler>()
                   .As<ICompiler>()
                   .SingleInstance();
        }
    }
}
