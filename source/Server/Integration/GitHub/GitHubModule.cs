using System;
using Autofac;
using JetBrains.Annotations;
using Octokit;

namespace SharpLab.WebApp.Integration.GitHub {
    [UsedImplicitly]
    public class GitHubModule : Module {
        public static bool Enabled { get; }

        protected override void Load(ContainerBuilder builder) {
            var clientId = Environment.GetEnvironmentVariable("SHARPLAB_GITHUB_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("SHARPLAB_GITHUB_CLIENT_SECRET");
            if (string.IsNullOrEmpty(clientId))
                return;

            if (string.IsNullOrEmpty(clientSecret))
                return;

            var settings = new GitHubClientSettings(clientId, clientSecret);
            builder.RegisterInstance(settings).AsSelf();

            var header = new ProductHeaderValue("SharpLab");
            builder.Register(_ => new GitHubClient(header))
                   .As<IGitHubClient>();
        }
    }
}
