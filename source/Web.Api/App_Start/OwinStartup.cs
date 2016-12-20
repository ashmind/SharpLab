using Microsoft.Owin;
using MirrorSharp;
using MirrorSharp.Owin;
using Owin;

[assembly: OwinStartup(typeof(TryRoslyn.Web.Api.OwinStartup), nameof(TryRoslyn.Web.Api.OwinStartup.Configuration))]

namespace TryRoslyn.Web.Api {
    public class OwinStartup {
        public void Configuration(IAppBuilder app) {
            app.UseMirrorSharp(new MirrorSharpOptions {
                SlowUpdateExtra = {
                    PrepareAsync = (session, token) => {
                        return null;
                    }
                }
            });
        }
    }
}