using System.Reflection;
using System.Web.Http;
using System.Web.Http.Cors;
using Autofac;
using Autofac.Integration.WebApi;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using TryRoslyn.Core;

namespace TryRoslyn.Web.Api {
    public static class WebApiConfig {
        public static void Register(HttpConfiguration config) {
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.EnableCors(new EnableCorsAttribute("*", "*", "*"));

            RegisterContainer(config);
            RegisterFormatters(config);
            
            // routes
            config.MapHttpAttributeRoutes();
            config.EnableSystemDiagnosticsTracing();
        }

        private static void RegisterContainer(HttpConfiguration config) {
            var builder = new ContainerBuilder();
            var webApiAssembly = Assembly.GetExecutingAssembly();

            builder.RegisterApiControllers(webApiAssembly);
            builder.RegisterAssemblyModules(typeof(ICodeProcessor).Assembly);
            builder.RegisterAssemblyModules(webApiAssembly);

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
