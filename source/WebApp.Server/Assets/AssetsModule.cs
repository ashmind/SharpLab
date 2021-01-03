using System;
using Autofac;

namespace SharpLab.WebApp.Server.Assets {
    public class AssetsModule : Module {
        protected override void Load(ContainerBuilder builder) {
            var latestUrl = Environment.GetEnvironmentVariable("SHARPLAB_ASSETS_LATEST_URL")
                         ?? throw new Exception("SHARPLAB_ASSETS_LATEST_URL was not found in the environment.");
            builder.RegisterType<LatestIndexHtmlProvider>()
                   .WithParameter(new NamedParameter("latestUrl", new Uri(latestUrl)))
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
