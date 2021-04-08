using System;
using System.IO;
using Autofac;

namespace SharpLab.WebApp.Server.Assets {
    public class AssetsModule : Module {
        protected override void Load(ContainerBuilder builder) {
            string GetRequiredEnvironmentVariable(string key) => Environment.GetEnvironmentVariable(key)
                ?? throw new Exception($"{key} was not found in the environment.");

            var baseUrl = GetRequiredEnvironmentVariable("SHARPLAB_ASSETS_BASE_URL");
            var latestUrl = GetRequiredEnvironmentVariable("SHARPLAB_ASSETS_LATEST_URL_V2");
            builder.RegisterType<LatestIndexHtmlProvider>()
                   .WithParameter(new NamedParameter("baseUrl", new Uri(baseUrl)))
                   .WithParameter(new NamedParameter("latestUrlAbsolute", new Uri(latestUrl)))
                   .As<IIndexHtmlProvider>()
                   .SingleInstance();

            var reloadToken = GetRequiredEnvironmentVariable("SHARPLAB_ASSETS_RELOAD_TOKEN");
            var errorHtml = File.ReadAllText("error.html");
            builder.RegisterType<IndexHtmlEndpoints>()
                   .WithParameter(new NamedParameter("reloadToken", reloadToken))
                   .WithParameter(new NamedParameter("errorHtml", errorHtml))
                   .AsSelf()
                   .SingleInstance();
        }
    }
}
