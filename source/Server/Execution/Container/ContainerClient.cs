using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLab.Server.Execution.Container {
    public class ContainerClient : IContainerClient {
        private readonly ContainerClientSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;

        public ContainerClient(ContainerClientSettings settings, IHttpClientFactory httpClientFactory) {
            _settings = settings;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ContainerExecutionResult> ExecuteAsync(string sessionId, Stream assemblyStream, bool includePerformance, CancellationToken cancellationToken) {
            var request = new HttpRequestMessage(HttpMethod.Post, _settings.ContainerHostUrl) {
                Headers = { { "SL-Session-Id", sessionId } },
                Content = new StreamContent(assemblyStream)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AuthorizationToken);
            if (includePerformance)
                request.Headers.Add("SL-Debug-Performance", "true");

            using var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.InternalServerError && response.Content.Headers.ContentType?.MediaType == "text/vnd.sharplab.error+plain")
                throw new Exception("Container host reported an error:\n" + await response.Content.ReadAsStringAsync(cancellationToken));

            response.EnsureSuccessStatusCode();
            var output = await response.Content.ReadAsStringAsync(cancellationToken);
            var outputFailed = response.Headers.TryGetValues("SL-Container-Output-Failed", out var l) && l.First() == "true";

            return new ContainerExecutionResult(output, outputFailed);
        }
    }
}
