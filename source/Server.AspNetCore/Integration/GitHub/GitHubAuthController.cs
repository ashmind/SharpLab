using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using SharpLab.WebApp.Integration.GitHub;

namespace SharpLab.Server.AspNetCore.Integration.GitHub {
    [Route("github/auth")]
    public class GitHubAuthController : ControllerBase {
        private readonly GitHubClientSettings _settings;
        private readonly Func<IGitHubClient> _clientFactory;

        public GitHubAuthController(GitHubClientSettings settings, Func<IGitHubClient> clientFactory) {
            _settings = settings;
            _clientFactory = clientFactory;
        }

        [HttpGet("start")]
        public RedirectResult Start() {
            var client = _clientFactory();
            var url = client.Oauth.GetGitHubLoginUrl(new OauthLoginRequest(_settings.ClientId) {
                Scopes = { "gist" }
            });
            return Redirect(url.ToString());
        }

        [HttpGet("complete")]
        public async Task<ContentResult> Complete(string code) {;
            var request = new OauthTokenRequest(_settings.ClientId, _settings.ClientSecret, code);
            var token = await _clientFactory().Oauth.CreateAccessToken(request).ConfigureAwait(false);

            return Content($@"<script>
                localStorage['sharplab.github.token'] = '{token.AccessToken}';
                window.location.href = sessionStorage['sharplab.github.auth.return'];
            </script>", MediaTypeNames.Text.Html);
        }
    }
}
