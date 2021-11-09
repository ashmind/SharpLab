using System;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpLab.Container.Manager;
using SharpLab.Container.Manager.Internal;

namespace SharpLab.Tests.Execution.Internal {
    [SupportedOSPlatform("windows")]
    public class ContainerManagerApiTestDriver {
        private static readonly AsyncLocal<DateTimeOffset> _now = new();
        private readonly CustomWebApplicationFactory _factory = new();

        static ContainerManagerApiTestDriver() {
            Environment.SetEnvironmentVariable("SHARPLAB_CONTAINER_HOST_AUTHORIZATION_TOKEN", "_");
        }

        public ContainerManagerApiTestDriver() {
            Client = _factory.CreateClient();
        }

        public DateTimeOffset Now {
            get => _now.Value;
            set => _now.Value = value;
        }

        public T Service<T>() where T: class => _factory.Services.GetRequiredService<T>();
        public HttpClient Client { get; }

        private class CustomWebApplicationFactory : WebApplicationFactory<Startup> {
            protected override IHost CreateHost(IHostBuilder builder) {
                builder.ConfigureServices(s => s.AddSingleton<IDateTimeProvider, TestDateTimeProvider>());
                return base.CreateHost(builder);
            }
        }

        private class TestDateTimeProvider : IDateTimeProvider {
            public DateTimeOffset GetNow() => _now.Value;
        }
    }
}
