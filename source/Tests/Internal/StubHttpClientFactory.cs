using System;
using System.Net.Http;

namespace SharpLab.Tests.Internal {
    // TODO: Rewrite
    public class StubHttpClientFactory : IHttpClientFactory {
        public HttpClient CreateClient(string name) => throw new NotSupportedException();
    }
}