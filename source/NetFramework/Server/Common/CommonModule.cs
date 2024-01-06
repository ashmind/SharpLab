using System;
using System.Net.Http;
using Autofac;
using JetBrains.Annotations;
using Microsoft.IO;
using Mono.Cecil.Cil;
using SharpLab.Server.Common.Internal;
using SharpLab.Server.Common.Languages;

namespace SharpLab.Server.Common {
    [UsedImplicitly]
    public class CommonModule : Module {
        protected override void Load(ContainerBuilder builder) {
            RegisterExternals(builder);

            builder.RegisterType<AssemblyReferenceCollector>()
                   .As<IAssemblyReferenceCollector>()
                   .SingleInstance();

            builder.RegisterType<PreCachedAssemblyResolver>()
                   .As<ICSharpCode.Decompiler.Metadata.IAssemblyResolver>()
                   .As<Mono.Cecil.IAssemblyResolver>()
                   .SingleInstance();

            builder.RegisterType<CSharpAdapter>()
                   .As<ILanguageAdapter>()
                   .SingleInstance();

            builder.RegisterType<VisualBasicAdapter>()
                   .As<ILanguageAdapter>()
                   .SingleInstance();

            builder.RegisterType<FSharpAdapter>()
                   .As<ILanguageAdapter>()
                   .SingleInstance();

            builder.RegisterType<ILAdapter>()
                   .As<ILanguageAdapter>()
                   .SingleInstance();

            var webAppName = EnvironmentHelper.GetRequiredEnvironmentVariable("SHARPLAB_WEBAPP_NAME");
            builder.RegisterType<FeatureTracker>()
                .As<IFeatureTracker>()
                .SingleInstance()
                .WithParameter("webAppName", webAppName);
        }

        private void RegisterExternals(ContainerBuilder builder) {
            builder.RegisterInstance(new RecyclableMemoryStreamManager())
                   .AsSelf();

            builder.RegisterInstance<Func<HttpClient>>(() => new HttpClient())
                   .As<Func<HttpClient>>()
                   .SingleInstance()
                   .PreserveExistingDefaults(); // allows tests and other overrides

            builder.RegisterType<PortablePdbReaderProvider>()
                   .As<ISymbolReaderProvider>()
                   .SingleInstance();
        }
    }
}
