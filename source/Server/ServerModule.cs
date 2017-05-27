using Autofac;
using Autofac.Core;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.IO;
using MirrorSharp.Advanced;
using SharpLab.Server.Compilation;
using SharpLab.Server.Compilation.Internal;
using SharpLab.Server.Compilation.Setups;
using SharpLab.Server.Decompilation;
using SharpLab.Server.Decompilation.AstOnly;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server {
    [UsedImplicitly]
    public class ServerModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<MetadataReferenceCollector>()
                   .As<IMetadataReferenceCollector>()
                   .SingleInstance();

            builder.RegisterType<CSharpFeatureDiscovery>()
                   .Keyed<IFeatureDiscovery>(LanguageNames.CSharp)
                   .SingleInstance();

            builder.RegisterType<VisualBasicFeatureDiscovery>()
                   .Keyed<IFeatureDiscovery>(LanguageNames.VisualBasic)
                   .SingleInstance();

            builder.RegisterType<CSharpSetup>()
                   .As<IMirrorSharpSetup>()
                   .WithParameter(ResolvedParameter.ForKeyed<IFeatureDiscovery>(LanguageNames.CSharp))
                   .SingleInstance();

            builder.RegisterType<VisualBasicSetup>()
                   .As<IMirrorSharpSetup>()
                   .WithParameter(ResolvedParameter.ForKeyed<IFeatureDiscovery>(LanguageNames.VisualBasic))
                   .SingleInstance();

            builder.RegisterType<FSharpSetup>()
                   .As<IMirrorSharpSetup>()
                   .SingleInstance();

            builder.RegisterType<Compiler>()
                   .As<ICompiler>()
                   .SingleInstance();

            builder.RegisterType<RoslynAstTarget>().As<IAstTarget>().SingleInstance();
            builder.RegisterType<FSharpAstTarget>().As<IAstTarget>().SingleInstance();

            builder.RegisterType<CSharpDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<VisualBasicDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<ILDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<JitAsmDecompiler>().As<IDecompiler>().SingleInstance();

            builder.RegisterInstance(new RecyclableMemoryStreamManager())
                   .AsSelf();

            builder.RegisterType<SlowUpdate>()
                   .As<ISlowUpdateExtension>()
                   .SingleInstance();

            builder.RegisterType<SetOptionsFromClient>()
                   .As<ISetOptionsFromClientExtension>()
                   .SingleInstance();
        }
    }
}