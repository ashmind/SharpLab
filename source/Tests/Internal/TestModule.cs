using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Pedantic.IO;

namespace SharpLab.Tests.Internal {
    public class TestModule : Module {
        protected override void Load(ContainerBuilder builder) {
            base.Load(builder);

            builder.RegisterInstance<Func<HttpClient>>(() => new HttpClient(new TestDataMessageHandler()))
                   .As<Func<HttpClient>>()
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
