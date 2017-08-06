using Autofac;
using Autofac.Core;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.IO;
using MirrorSharp.Advanced;
using SharpLab.Server.Compilation;
using SharpLab.Server.Compilation.Internal;
using SharpLab.Server.Decompilation;
using SharpLab.Server.Decompilation.AstOnly;
using SharpLab.Server.Execution;
using SharpLab.Server.Execution.Internal;
using SharpLab.Server.MirrorSharp;
using SharpLab.Server.MirrorSharp.Internal;
using SharpLab.Server.MirrorSharp.Internal.Languages;
using SharpLab.Server.Monitoring;

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

            builder.RegisterType<CSharpIntegration>()
                   .As<ILanguageIntegration>()
                   .WithParameter(ResolvedParameter.ForKeyed<IFeatureDiscovery>(LanguageNames.CSharp))
                   .SingleInstance();

            builder.RegisterType<VisualBasicIntegration>()
                   .As<ILanguageIntegration>()
                   .WithParameter(ResolvedParameter.ForKeyed<IFeatureDiscovery>(LanguageNames.VisualBasic))
                   .SingleInstance();

            builder.RegisterType<FSharpIntegration>()
                   .As<ILanguageIntegration>()
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

            builder.RegisterType<Executor>()
                   .As<IExecutor>()
                   .SingleInstance();
            builder.RegisterType<FlowReportingRewriter>()
                   .As<IAssemblyRewriter>()
                   .SingleInstance();
            builder.RegisterType<FSharpEntryPointRewriter>()
                   .As<IAssemblyRewriter>()
                   .SingleInstance();

            builder.RegisterInstance(new RecyclableMemoryStreamManager())
                   .AsSelf();

            builder.RegisterType<DefaultTraceMonitor>()
                   .As<IMonitor>()
                   .SingleInstance()
                   .PreserveExistingDefaults();

            builder.RegisterType<MonitorExceptionLogger>()
                   .As<IExceptionLogger>()
                   .SingleInstance();

            builder.RegisterType<SlowUpdate>()
                   .As<ISlowUpdateExtension>()
                   .SingleInstance();

            builder.RegisterType<SetOptionsFromClient>()
                   .As<ISetOptionsFromClientExtension>()
                   .SingleInstance();
        }
    }
}