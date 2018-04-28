using System;
using System.Configuration;
using System.Net.Http;
using Microsoft.IO;
using Autofac;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Microsoft.CodeAnalysis;
using SourcePath;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Internal;
using SharpLab.Server.Common.Languages;
using SharpLab.Server.Compilation;
using SharpLab.Server.Decompilation;
using SharpLab.Server.Decompilation.AstOnly;
using SharpLab.Server.Execution;
using SharpLab.Server.Execution.Internal;
using SharpLab.Server.MirrorSharp;
using SharpLab.Server.Monitoring;
using SharpLab.Server.Explanation;
using SharpLab.Server.Explanation.Internal;
using System.Collections.Generic;
using SourcePath.Roslyn;

namespace SharpLab.Server {
    [UsedImplicitly]
    public class ServerModule : Module {
        protected override void Load(ContainerBuilder builder) {
            RegisterSourcePath(builder);
            RegisterCommon(builder);

            builder.RegisterType<Compiler>()
                   .As<ICompiler>()
                   .SingleInstance();

            RegisterDecompilation(builder);
            RegisterExecution(builder);
            RegisterExplanation(builder);

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

        private void RegisterSourcePath(ContainerBuilder builder) {
            builder.RegisterType<RoslynAxisNavigator>()
                   .As<ISourcePathAxisNavigator<SyntaxNodeOrToken>>()
                   .SingleInstance();

            builder.RegisterType<CSharpExplanationPathDialect>()
                   .As<ISourcePathDialect<SyntaxNodeOrToken>>()
                   .SingleInstance();

            builder.RegisterType<SourcePathParser<SyntaxNodeOrToken>>()
                   .As<ISourcePathParser<SyntaxNodeOrToken>>()
                   .SingleInstance();
        }

        private static void RegisterCommon(ContainerBuilder builder) {
            builder.RegisterInstance(new RecyclableMemoryStreamManager())
                   .AsSelf();

            builder.RegisterInstance<Func<HttpClient>>(() => new HttpClient())
                   .As<Func<HttpClient>>()
                   .SingleInstance()
                   .PreserveExistingDefaults(); // allows tests and other overrides

            builder.RegisterType<AssemblyReferenceCollector>()
                   .As<IAssemblyReferenceCollector>()
                   .SingleInstance();

            builder.RegisterType<PreCachedAssemblyResolver>()
                   .As<IAssemblyResolver>()
                   .SingleInstance();

            builder.RegisterType<NativePdbReaderProvider>()
                   .As<ISymbolReaderProvider>()
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
        }

        private static void RegisterDecompilation(ContainerBuilder builder) {
            builder.RegisterType<RoslynAstTarget>().As<IAstTarget>().SingleInstance();
            builder.RegisterType<FSharpAstTarget>().As<IAstTarget>().SingleInstance();

            builder.RegisterType<CSharpDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<ILDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<JitAsmDecompiler>().As<IDecompiler>().SingleInstance();
        }

        private static void RegisterExecution(ContainerBuilder builder) {
            builder.RegisterType<Executor>()
                   .As<IExecutor>()
                   .SingleInstance();

            builder.RegisterType<FlowReportingRewriter>()
                   .As<IAssemblyRewriter>()
                   .SingleInstance();

            builder.RegisterType<FSharpEntryPointRewriter>()
                   .As<IAssemblyRewriter>()
                   .SingleInstance();
        }

        private static void RegisterExplanation(ContainerBuilder builder) {
            var csharpSourceUrl = new Uri(ConfigurationManager.AppSettings["App.Explanations.Urls.CSharp"]);
            var updatePeriod = TimeSpan.Parse(ConfigurationManager.AppSettings["App.Explanations.UpdatePeriod"]);

            builder.RegisterType<Explainer>()
                   .As<IExplainer>()
                   .SingleInstance();

            builder.RegisterType<ExternalSyntaxExplanationProvider>()
                   .As<ISyntaxExplanationProvider>()
                   .WithParameter("sourceUrl", csharpSourceUrl)
                   .WithParameter("updatePeriod", updatePeriod)
                   .SingleInstance();
        }
    }
}