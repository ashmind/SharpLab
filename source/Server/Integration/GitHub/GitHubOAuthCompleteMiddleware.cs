using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Octokit;

namespace SharpLab.WebApp.Middleware.GitHub {
    public class GitHubOAuthCompleteMiddleware : OwinMiddleware {
        private readonly Func<IGitHubClient> _clientFactory;
        private readonly GitHubClientSettings _settings;

        public GitHubOAuthCompleteMiddleware(OwinMiddleware next, GitHubClientSettings settings, Func<IGitHubClient> clientFactory) : base(next) {
            _clientFactory = clientFactory;
            _settings = settings;
        }

        public async override Task Invoke(IOwinContext context) {
            var code = context.Request.Query["code"];
            var request = new OauthTokenRequest(_settings.ClientId, _settings.ClientSecret, code);
            var token = await _clientFactory().Oauth.CreateAccessToken(request).ConfigureAwait(false);

            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync($@"<script>
                localStorage['sharplab.github.token'] = '{token.AccessToken}';
                window.location.href = sessionStorage['sharplab.github.auth.return'];
            </script>").ConfigureAwait(false);
        }
    }
}
