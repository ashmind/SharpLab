using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SharpLab.WebApp.Server.Assets {
    // Not using controller for this to avoid per-request allocations on a hot path
    public class IndexHtmlEndpoints {
        private readonly IIndexHtmlProvider _provider;
        private ReadOnlyMemory<byte> _indexHtmlBytes;
        private readonly string _reloadAuthorization;

        public IndexHtmlEndpoints(IIndexHtmlProvider provider, string reloadToken) {
            _provider = provider;
            _reloadAuthorization = $"Bearer {reloadToken}";
        }

        public Task StartAsync() => UpdateBytesAsync();

        public async Task GetRootAsync(HttpContext context) {
            context.Response.ContentType = "text/html";
            await context.Response.BodyWriter.WriteAsync(_indexHtmlBytes);
        }

        public async Task PostReloadAsync(HttpContext context) {
            if (context.Request.Headers["Authorization"] != _reloadAuthorization) {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            await UpdateBytesAsync();
            context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            return;
        }

        private async Task UpdateBytesAsync() {
            _indexHtmlBytes = Encoding.UTF8.GetBytes(await _provider.GetIndexHtmlContentAsync()).AsMemory();
        }
    }
}
