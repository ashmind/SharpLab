using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SharpLab.WebApp.Server.Assets {
    public class LatestIndexHtmlProvider : IIndexHtmlProvider {
        private readonly Uri _baseUrl;
        private readonly IHttpClientFactory _httpClientFactory;

        public LatestIndexHtmlProvider(Uri baseUrl, IHttpClientFactory httpClientFactory) {
            _baseUrl = baseUrl;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> GetIndexHtmlContentAsync() {
            var (htmlUrl, htmlString) = await GetRawIndexHtmlAsync();

            var html = new HtmlDocument();
            html.LoadHtml(htmlString);

            foreach (var link in html.DocumentNode.Descendants("link")) {
                AdjustUrlAttributeIfAny(link, "href", htmlUrl);
            }

            foreach (var script in html.DocumentNode.Descendants("script")) {
                AdjustUrlAttributeIfAny(script, "src", htmlUrl);
            }

            return html.DocumentNode.OuterHtml.ToString();
        }

        private void AdjustUrlAttributeIfAny(HtmlNode element, string attributeName, Uri baseUrl) {
            var urlString = element.GetAttributeValue(attributeName, null);
            if (urlString == null)
                return;

            var url = new Uri(urlString, UriKind.RelativeOrAbsolute);
            if (url.IsAbsoluteUri)
                return;

            element.SetAttributeValue(attributeName, new Uri(baseUrl, url).ToString());
        }

        private async Task<(Uri url, string content)> GetRawIndexHtmlAsync() {
            using var client = _httpClientFactory.CreateClient();

            var latestUrlRelative = await client.GetStringAsync(new Uri(_baseUrl, "latest"));
            var latestUrl = new Uri(_baseUrl, new Uri(latestUrlRelative, UriKind.Relative));

            return (latestUrl, await client.GetStringAsync(latestUrl));
        }
    }
}
