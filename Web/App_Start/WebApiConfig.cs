using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using TryRoslyn.Web.Formatting;

namespace TryRoslyn.Web {
    public static class WebApiConfig {
        public static void Register(HttpConfiguration config) {
            // formatters
            RegisterFormatters(config);

            // routes
            config.MapHttpAttributeRoutes();
            config.EnableSystemDiagnosticsTracing();
        }

        private static void RegisterFormatters(HttpConfiguration config) {
            config.Formatters.Add(new CodeMediaTypeFormatter());

            var jsonSettings = config.Formatters.JsonFormatter.SerializerSettings;
            jsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonSettings.Converters.Add(new StringEnumConverter());
            jsonSettings.Converters.Add(new SyntaxTreeConverter());
        }
    }
}
