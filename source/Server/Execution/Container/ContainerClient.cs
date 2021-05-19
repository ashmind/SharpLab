using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLab.Server.Execution.Container {
    public class ContainerClient {
        private readonly ContainerClientSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;

        public ContainerClient(ContainerClientSettings settings, IHttpClientFactory httpClientFactory) {
            _settings = settings;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> ExecuteAsync(string sessionId, Stream assemblyStream, CancellationToken cancellationToken) {
            var request = new HttpRequestMessage(HttpMethod.Post, _settings.RunnerUrl) {
                Headers = {{ "Sl-Session-Id", sessionId }},
                Content = new StreamContent(assemblyStream)
            };

            using var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
