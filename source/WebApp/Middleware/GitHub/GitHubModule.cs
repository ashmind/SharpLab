using System;
using Autofac;
using JetBrains.Annotations;
using Octokit;

namespace SharpLab.WebApp.Middleware.GitHub {
    [UsedImplicitly]
    public class GitHubModule : Module {
        protected override void Load(ContainerBuilder builder) {
            var clientId = Environment.GetEnvironmentVariable("SHARPLAB_GITHUB_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("SHARPLAB_GITHUB_CLIENT_SECRET");
            var settings = new GitHubClientSettings(clientId, clientSecret);
            builder.RegisterInstance(settings).AsSelf();

            var header = new ProductHeaderValue("SharpLab");
            builder.Register(_ => new GitHubClient(header))
                   .As<IGitHubClient>();

            builder.RegisterType<GitHubOAuthStartMiddleware>().AsSelf();
            builder.RegisterType<GitHubOAuthCompleteMiddleware>().AsSelf();
        }
    }
}
