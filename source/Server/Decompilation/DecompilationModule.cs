using System;
using Autofac;
using Autofac.Core;
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
            builder.RegisterType<FSharpAstTarget>()
                   .As<IAstTarget>()
                   .SingleInstance();

            builder.RegisterType<CSharpDecompiler>()
                   .As<IDecompiler>()
                   .SingleInstance();
            builder.RegisterType<ILDecompiler>()
                   .As<IDecompiler>()
                   .As<IILDecompiler>()
                   .SingleInstance();
            builder.RegisterType<ExecutionILDecompiler>()
                   .As<IDecompiler>()
                   .SingleInstance();

            builder.RegisterInstance(new JitAsmSettings(
                shouldDisableMethodSymbolResolver: Environment.GetEnvironmentVariable("SHARPLAB_JITASM_DISABLE_METHOD_RESOLVER") is {} d
                    ? bool.Parse(d)
                    : false
            )).AsSelf();

            builder.RegisterType<JitAsmDecompiler>()
                   .As<IDecompiler>()
                   .SingleInstance();
        }
    }
}
