using Autofac;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using SharpLab.Server.Compilation.Guards;
using SharpLab.Server.Compilation.Internal.Guards;

namespace SharpLab.Server.Compilation {
    [UsedImplicitly]
    public class CompilationModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<Compiler>()
                   .As<ICompiler>()
                   .SingleInstance();

            builder.RegisterType<CSharpGuard>()
                   .As<IRoslynGuardInternal<CSharpCompilation>>()
                   .SingleInstance();

            builder.RegisterType<VisualBasicGuard>()
                   .As<IRoslynGuardInternal<VisualBasicCompilation>>()
                   .SingleInstance();
        }
    }
}
