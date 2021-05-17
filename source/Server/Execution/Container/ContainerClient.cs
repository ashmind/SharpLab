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

        public async Task<string> ExecuteAsync(Stream assemblyStream, CancellationToken cancellationToken) {
            using var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.PostAsync(_settings.RunnerUrl, new StreamContent(assemblyStream), cancellationToken);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
