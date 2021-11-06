using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;
using Pedantic.IO;
using SharpLab.Server.Caching.Internal;
using SharpLab.Server.Caching.Internal.Mocks;
using SharpLab.Server.Execution.Container;
using SharpLab.Server.Execution.Container.Mocks;

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

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { "App:Explanations:Urls:CSharp", "http://testdata/language-syntax-explanations/csharp.yml" },
                    { "App:Explanations:UpdatePeriod", "01:00:00" }
                })
                .Build();
            builder.RegisterInstance<IConfiguration>(configuration)
                   .SingleInstance();

            RegisterMock<IResultCacheStore, ResultCacheStoreMock>(builder, (wrapper, target) =>
                wrapper.Setup.StoreAsync().Runs((key, stream, cancellationToken) => target.Value?.StoreAsync(key, stream, cancellationToken) ?? Task.CompletedTask)
            );
            RegisterMock<IContainerClient, ContainerClientMock>(builder, (wrapper, target) =>
                wrapper.Setup.ExecuteAsync().Runs((sessionId, assemblyStream, includePerformance, cancellationToken) => target.Value!.ExecuteAsync(sessionId, assemblyStream, includePerformance, cancellationToken))
            );
        }

        private void RegisterMock<T, TMock>(ContainerBuilder builder, Action<TMock, AsyncLocal<TMock>> setupSingletonWrapper)
            where T : class
            where TMock: T, new()
        {
            var mockPerTest = new AsyncLocal<TMock>();
            var singletonWrapper = new TMock();
            setupSingletonWrapper(singletonWrapper, mockPerTest);

            builder.Register(_ => mockPerTest.Value ??= new());
            builder.RegisterInstance<T>(singletonWrapper);
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
