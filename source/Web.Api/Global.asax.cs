using System.Web.Http;

namespace TryRoslyn.Web.Api {
    public class WebApiApplication : System.Web.HttpApplication {
        protected void Application_Start() {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_End() {
            GlobalConfiguration.Configuration.Dispose();
        }
    }
}
