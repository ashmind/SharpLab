using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SharpLab.WebApp.Server.Assets {
    public class LatestIndexHtmlProvider : IIndexHtmlProvider {
        // This is normally the CDN URL
        private readonly Uri _baseUrl;
        // This is normally the blob URL for /latest (that contains relative URL for index.html)
        private readonly Uri _latestUrlAbsolute;
        private readonly IHttpClientFactory _httpClientFactory;

        public LatestIndexHtmlProvider(Uri baseUrl, Uri latestUrlAbsolute, IHttpClientFactory httpClientFactory) {
            _baseUrl = baseUrl;
            _latestUrlAbsolute = latestUrlAbsolute;
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

            var indexUrlRelative = await client.GetStringAsync(_latestUrlAbsolute);
            var indexUrl = new Uri(_baseUrl, new Uri(indexUrlRelative, UriKind.Relative));

            return (indexUrl, await client.GetStringAsync(indexUrl));
        }
    }
}
