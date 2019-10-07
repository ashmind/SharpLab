using System;
using Autofac;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using SharpLab.Server.Explanation.Internal;
using SourcePath;
using SourcePath.Roslyn;

namespace SharpLab.Server.Explanation {
    [UsedImplicitly]
    public class ExplanationModule : Module {
        protected override void Load(ContainerBuilder builder) {
            RegisterSourcePath(builder);

            builder.RegisterType<Explainer>()
                   .As<IExplainer>()
                   .SingleInstance();

            builder.RegisterType<ExternalSyntaxExplanationProvider>()
                   .As<ISyntaxExplanationProvider>()
                   .SingleInstance();

            builder.Register(c => {
                var configuration = c.Resolve<IConfiguration>();
                return new ExternalSyntaxExplanationSettings(
                    configuration.GetValue<Uri>("App:Explanations:Urls:CSharp"),
                    configuration.GetValue<TimeSpan>("App:Explanations:UpdatePeriod")
                );
            }).SingleInstance();
        }

        private void RegisterSourcePath(ContainerBuilder builder) {
            builder.RegisterType<RoslynNodeHandler>()
                   .As<ISourceNodeHandler<RoslynNodeContext>>()
                   .SingleInstance();

            builder.RegisterType<CSharpExplanationPathDialect>()
                   .As<ISourcePathDialect<RoslynNodeContext>>()
                   .SingleInstance();

            builder.RegisterType<SourcePathParser<RoslynNodeContext>>()
                   .As<ISourcePathParser<RoslynNodeContext>>()
                   .SingleInstance();
        }
    }
}
