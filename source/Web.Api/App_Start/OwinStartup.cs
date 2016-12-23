using Autofac;
using Microsoft.Owin;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.Owin;
using Owin;

[assembly: OwinStartup(typeof(TryRoslyn.Web.Api.OwinStartup), nameof(TryRoslyn.Web.Api.OwinStartup.Configuration))]

namespace TryRoslyn.Web.Api {
    public class OwinStartup {
        public void Configuration(IAppBuilder app) {
            RegisterMirrorSharp(app);
        }

        private static void RegisterMirrorSharp(IAppBuilder app, IContainer container) {

            var options = new MirrorSharpOptions {
                SlowUpdate = container.Resolve<ICustomSlowUpdate>()
            };

            app.UseMirrorSharp(options);
        }
    }
}