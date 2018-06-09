using System;
using System.Configuration;
using Autofac;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using SharpLab.Server.Explanation.Internal;
using SourcePath;
using SourcePath.Roslyn;

namespace SharpLab.Server.Explanation {
    [UsedImplicitly]
    public class ExplanationModule : Module {
        protected override void Load(ContainerBuilder builder) {
            var csharpSourceUrl = new Uri(ConfigurationManager.AppSettings["App.Explanations.Urls.CSharp"]);
            var updatePeriod = TimeSpan.Parse(ConfigurationManager.AppSettings["App.Explanations.UpdatePeriod"]);

            RegisterSourcePath(builder);

            builder.RegisterType<Explainer>()
                   .As<IExplainer>()
                   .SingleInstance();

            builder.RegisterType<ExternalSyntaxExplanationProvider>()
                   .As<ISyntaxExplanationProvider>()
                   .WithParameter("sourceUrl", csharpSourceUrl)
                   .WithParameter("updatePeriod", updatePeriod)
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
    }
}
