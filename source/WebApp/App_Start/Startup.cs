using Microsoft.Owin;
using Owin;
using SharpLab.WebApp;
using SharpLab.WebApp.Middleware.GitHub;

[assembly: OwinStartup("WebApp", typeof(Startup), nameof(Startup.Configuration))]

namespace SharpLab.WebApp {
    public class Startup : Server.Startup {
        public override void Configuration(IAppBuilder app) {
            base.Configuration(app);
            app.Map("/github/auth/start", a => a.UseMiddlewareFromContainer<GitHubOAuthStartMiddleware>());
            app.Map("/github/auth/complete", a => a.UseMiddlewareFromContainer<GitHubOAuthCompleteMiddleware>());
        }
    }
}
