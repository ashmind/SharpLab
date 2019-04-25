using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Microsoft.Extensions.Configuration;

namespace SharpLab.Tests.Internal {
    public class NetCoreTestModule : Module {
        protected override void Load(ContainerBuilder builder) {
            base.Load(builder);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { "App:Explanations:Urls:CSharp", "http://testdata/language-syntax-explanations/csharp.yml" },
                    { "App:Explanations:UpdatePeriod", "01:00:00" }
                })
                .Build();

            builder.RegisterInstance<IConfiguration>(configuration)
                   .SingleInstance();
        }
    }
}
