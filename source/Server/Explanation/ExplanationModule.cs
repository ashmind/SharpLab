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
                    // TODO: Theoretically this needs a helper, but I am looking to deprecate explanations
                    // feature anyways.
                    configuration.GetValue<Uri>("App:Explanations:Urls:CSharp")
                        ?? throw new ("Setting 'App:Explanations:Urls:CSharp' was not found"),
                    configuration.GetValue<TimeSpan?>("App:Explanations:UpdatePeriod")
                        ?? throw new("Setting 'App:Explanations:UpdatePeriod' was not found")
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
