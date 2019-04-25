using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Octokit;

namespace SharpLab.WebApp.Middleware.GitHub {
    public class GitHubOAuthStartMiddleware : OwinMiddleware {
        private readonly GitHubClientSettings _settings;
        private readonly Func<IGitHubClient> _clientFactory;

        public GitHubOAuthStartMiddleware(OwinMiddleware next, GitHubClientSettings settings, Func<IGitHubClient> clientFactory) : base(next) {
            _settings = settings;
            _clientFactory = clientFactory;
        }

        public override Task Invoke(IOwinContext context) {
            var client = _clientFactory();
            var url = client.Oauth.GetGitHubLoginUrl(new OauthLoginRequest(_settings.ClientId) {
                Scopes = { "gist" }
            });
            context.Response.Redirect(url.ToString());
            return Task.CompletedTask;
        }
    }
}
