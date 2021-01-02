using System;
using Autofac;

namespace SharpLab.WebApp.Server.Assets {
    public class AssetsModule : Module {
        protected override void Load(ContainerBuilder builder) {
            var baseUrl = Environment.GetEnvironmentVariable("SHARPLAB_ASSETS_BASE_URL")
                       ?? throw new Exception("SHARPLAB_ASSETS_BASE_URL was not found in the environment.");
            builder.RegisterType<LatestIndexHtmlProvider>()
                   .WithParameter(new NamedParameter("baseUrl", new Uri(baseUrl)))
                   .As<IIndexHtmlProvider>()
                   .SingleInstance();

            var reloadToken = Environment.GetEnvironmentVariable("SHARPLAB_ASSETS_RELOAD_TOKEN")
                           ?? throw new Exception("SHARPLAB_ASSETS_RELOAD_TOKEN was not found in the environment.");
            builder.RegisterType<IndexHtmlEndpoints>()
                   .WithParameter(new NamedParameter("reloadToken", reloadToken))
                   .AsSelf()                   
                   .SingleInstance();
        }
    }
}
