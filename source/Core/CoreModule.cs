using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using TryRoslyn.Core.Decompilation;
using TryRoslyn.Core.Processing;
using TryRoslyn.Core.Processing.Languages;
using TryRoslyn.Core.Processing.Languages.Internal;

namespace TryRoslyn.Core {
    public class CoreModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<MetadataReferenceCollector>()
                   .As<IMetadataReferenceCollector>()
                   .SingleInstance();

            builder.RegisterType<CSharpFeatureDiscovery>()
                   .Keyed<IFeatureDiscovery>(LanguageIdentifier.CSharp)
                   .SingleInstance();

            builder.RegisterType<VBNetFeatureDiscovery>()
                   .Keyed<IFeatureDiscovery>(LanguageIdentifier.VBNet)
                   .SingleInstance();

            builder.RegisterType<CSharpLanguage>()
                   .As<IRoslynLanguage>()
                   .WithParameter(ResolvedParameter.ForKeyed<IFeatureDiscovery>(LanguageIdentifier.CSharp))
                   .SingleInstance();

            builder.RegisterType<VBNetLanguage>()
                   .As<IRoslynLanguage>()
                   .WithParameter(ResolvedParameter.ForKeyed<IFeatureDiscovery>(LanguageIdentifier.VBNet))
                   .SingleInstance();

            builder.RegisterType<CSharpDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<VBNetDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<ILDecompiler>().As<IDecompiler>().SingleInstance();
        }
    }
}