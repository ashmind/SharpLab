using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;
using Pedantic.IO;
using SharpLab.Server.Caching.Internal;
using SharpLab.Server.Caching.Internal.Mocks;
using SharpLab.Server.Execution.Unbreakable;
using Unbreakable;

namespace SharpLab.Tests.Internal {
    public class TestModule : Module {
        protected override void Load(ContainerBuilder builder) {
            base.Load(builder);

            builder.RegisterType<StubHttpClientFactory>()
                   .As<IHttpClientFactory>()
                   .SingleInstance();

            builder.RegisterInstance<Func<HttpClient>>(() => new HttpClient(new TestDataMessageHandler()))
                   .As<Func<HttpClient>>()
                   .SingleInstance();

            var testApiPolicy = ApiPolicySetup.CreatePolicy()
                .Namespace("System.Globalization", ApiAccess.Neutral, n => n.Type(typeof(CultureInfo), ApiAccess.Neutral, t => t.Setter(nameof(CultureInfo.CurrentCulture), ApiAccess.Allowed)));
            builder.RegisterInstance(testApiPolicy)
                   .AsSelf()
                   .SingleInstance();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { "App:Explanations:Urls:CSharp", "http://testdata/language-syntax-explanations/csharp.yml" },
                    { "App:Explanations:UpdatePeriod", "01:00:00" }
                })
                .Build();
            builder.RegisterInstance<IConfiguration>(configuration)
                   .SingleInstance();

            RegisterResultCacheStoreMock(builder);
        }

        private void RegisterResultCacheStoreMock(ContainerBuilder builder) {
            var mockPerTest = new AsyncLocal<ResultCacheStoreMock>();
            var singletonWrapper = new ResultCacheStoreMock();
            singletonWrapper.Setup.StoreAsync()
                .Runs((key, stream, cancellationToken) => mockPerTest.Value?.StoreAsync(key, stream, cancellationToken) ?? Task.CompletedTask);

            builder.Register(_ => mockPerTest.Value ??= new());
            builder.RegisterInstance<IResultCacheStore>(singletonWrapper);

        }

        private class TestDataMessageHandler : HttpMessageHandler {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
                if (request.RequestUri?.Authority != "testdata")
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
