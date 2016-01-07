using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using TryRoslyn.Core;
using TryRoslyn.Core.Modules;
using TryRoslyn.Web.Formatting;

namespace TryRoslyn.Web {
    public static class WebApiConfig {
        public static void Register(HttpConfiguration config) {
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            RegisterContainer(config);
            RegisterFormatters(config);
            
            // routes
            config.MapHttpAttributeRoutes();
            config.EnableSystemDiagnosticsTracing();
        }

        private static void RegisterContainer(HttpConfiguration config) {
            var builder = new ContainerBuilder();
            var webAssembly = Assembly.GetExecutingAssembly();

            builder.RegisterApiControllers(webAssembly);
            builder.RegisterAssemblyModules(typeof(ICodeProcessor).Assembly);
            builder.RegisterAssemblyModules(webAssembly);

            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }

        private static void RegisterFormatters(HttpConfiguration config) {
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            var jsonSettings = config.Formatters.JsonFormatter.SerializerSettings;
            jsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonSettings.Converters.Add(new StringEnumConverter());
            //jsonSettings.Converters.Add(new SyntaxTreeConverter());
        }
    }
}
