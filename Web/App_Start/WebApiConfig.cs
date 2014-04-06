using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using TryRoslyn.Web.Formatters;

namespace TryRoslyn.Web {
    public static class WebApiConfig {
        public static void Register(HttpConfiguration config) {
            // Web API configuration and services
            config.Formatters.Add(new CodeMediaTypeFormatter());

            // Web API routes
            config.MapHttpAttributeRoutes();
            config.EnableSystemDiagnosticsTracing();
        }
    }
}
