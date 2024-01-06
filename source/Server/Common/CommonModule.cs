using System;
using System.Net.Http;
using Autofac;
using JetBrains.Annotations;
using Microsoft.IO;
using Mono.Cecil.Cil;
using SharpLab.Server.Common.Internal;
using SharpLab.Server.Common.Languages;
using SharpLab.Server.Compilation;

namespace SharpLab.Server.Common; 
[UsedImplicitly]
public class CommonModule : Module {
    protected override void Load(ContainerBuilder builder) {
        RegisterExternals(builder);

        builder.RegisterInstance(MemoryPoolSlim<byte>.Shared);

        builder.RegisterType<ExceptionLogFilter>()
               .As<IExceptionLogFilter>()
               .SingleInstance();

        builder.RegisterType<AssemblyPathCollector>()
               .As<IAssemblyPathCollector>()
               .SingleInstance();

        builder.RegisterType<PreCachedAssemblyResolver>()
               .As<ICSharpCode.Decompiler.Metadata.IAssemblyResolver>()
               .As<Mono.Cecil.IAssemblyResolver>()
               .SingleInstance();

        builder.RegisterType<LocalAssemblyDocumentationResolver>()
               .As<IAssemblyDocumentationResolver>()
               .SingleInstance();

        builder.RegisterType<CSharpAdapter>()
               .As<ILanguageAdapter>()
               .SingleInstance();

        builder.RegisterType<CSharpTopLevelProgramSupport>()
               .As<ICSharpTopLevelProgramSupport>()
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

        RegisterConfiguration(builder);

        var webAppName = EnvironmentHelper.GetRequiredEnvironmentVariable("SHARPLAB_WEBAPP_NAME");
        builder.RegisterType<FeatureTracker>()
            .As<IFeatureTracker>()
            .SingleInstance()
            .WithParameter("webAppName", webAppName);
    }

    private void RegisterConfiguration(ContainerBuilder builder) {
        builder.RegisterType<LocalSecretsClient>()
               .As<ISecretsClient>()
               .SingleInstance()
               .PreserveExistingDefaults();

        builder.RegisterType<LocalFeatureFlagClient>()
               .As<IFeatureFlagClient>()
               .SingleInstance()
               .PreserveExistingDefaults();
    }

    private void RegisterExternals(ContainerBuilder builder) {
        builder.RegisterInstance(new RecyclableMemoryStreamManager())
               .AsSelf();

        // older approach, needs to be updated to use factory now
        builder.RegisterInstance<Func<HttpClient>>(() => new HttpClient())
               .As<Func<HttpClient>>()
               .SingleInstance()
               .PreserveExistingDefaults(); // allows tests and other overrides

        builder.RegisterType<PortablePdbReaderProvider>()
               .As<ISymbolReaderProvider>()
               .SingleInstance();
    }
}
