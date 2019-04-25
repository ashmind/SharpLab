using Autofac;
using JetBrains.Annotations;
using SharpLab.Server.Decompilation.AstOnly;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    [UsedImplicitly]
    public class DecompilationModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<RoslynOperationPropertySerializer>()
                   .As<IRoslynOperationPropertySerializer>()
                   .SingleInstance();
            builder.RegisterType<PortablePdbDebugInfoProvider>()
                   .As<IDisposableDebugInfoProvider>()
                   .InstancePerDependency();

            builder.RegisterType<RoslynAstTarget>()
                   .As<IAstTarget>()
                   .SingleInstance();
            #if !NETCOREAPP
            builder.RegisterType<FSharpAstTarget>()
                   .As<IAstTarget>()
                   .SingleInstance();
            #endif

            builder.RegisterType<CSharpDecompiler>()
                   .As<IDecompiler>()
                   .SingleInstance();
            builder.RegisterType<ILDecompiler>()
                   .As<IDecompiler>()
                   .SingleInstance();
        }
    }
}
