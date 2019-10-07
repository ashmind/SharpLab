using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Pedantic.IO;
using SharpLab.Server.Execution.Unbreakable;
using Unbreakable;

namespace SharpLab.Tests.Internal {
    public class TestModule : Module {
        protected override void Load(ContainerBuilder builder) {
            base.Load(builder);

            builder.RegisterInstance<Func<HttpClient>>(() => new HttpClient(new TestDataMessageHandler()))
                   .As<Func<HttpClient>>()
                   .SingleInstance();

            var testApiPolicy = ApiPolicySetup.CreatePolicy()
                .Namespace("System.Globalization", ApiAccess.Neutral, n => n.Type(typeof(CultureInfo), ApiAccess.Neutral, t => t.Setter(nameof(CultureInfo.CurrentCulture), ApiAccess.Allowed)));
            builder.RegisterInstance(testApiPolicy)
                   .AsSelf()
                   .SingleInstance();
        }

        private class TestDataMessageHandler : HttpMessageHandler {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
                if (request.RequestUri.Authority != "testdata")
                    throw new NotSupportedException();

                var resourcePath = "TestData" + request.RequestUri.LocalPath
                    .Replace('/', '.')
                    .Replace('-', '_');

                var resourceText = EmbeddedResource.ReadAllText(typeof(ExecutionTests), resourcePath);
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
                    Content = new StringContent(resourceText)
                });
            }
        }
    }
}
