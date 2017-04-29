using Autofac;
using Autofac.Core;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.IO;
using MirrorSharp.Advanced;
using TryRoslyn.Server.Compilation;
using TryRoslyn.Server.Compilation.Internal;
using TryRoslyn.Server.Decompilation;
using TryRoslyn.Server.MirrorSharp;

namespace TryRoslyn.Server {
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
                   .As<ILanguageSetup>()
                   .WithParameter(ResolvedParameter.ForKeyed<IFeatureDiscovery>(LanguageNames.CSharp))
                   .SingleInstance();

            builder.RegisterType<VisualBasicSetup>()
                   .As<ILanguageSetup>()
                   .WithParameter(ResolvedParameter.ForKeyed<IFeatureDiscovery>(LanguageNames.VisualBasic))
                   .SingleInstance();

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