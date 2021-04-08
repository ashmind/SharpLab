using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SharpLab.WebApp.Server.Assets {
    // Not using controller for this to avoid per-request allocations on a hot path
    public class IndexHtmlEndpoints {
        private readonly IIndexHtmlProvider _provider;
        private readonly ILogger<IndexHtmlEndpoints> _logger;
        private readonly SemaphoreSlim _initialLoadSemaphore = new SemaphoreSlim(1, 1);
        private readonly ReadOnlyMemory<byte> _errorHtmlBytes;
        private ReadOnlyMemory<byte>? _indexHtmlBytes;
        private readonly string _reloadAuthorization;

        public IndexHtmlEndpoints(
            IIndexHtmlProvider provider,
            string reloadToken,
            string errorHtml,
            ILogger<IndexHtmlEndpoints> logger
        ) {
            _provider = provider;
            _errorHtmlBytes = Encoding.UTF8.GetBytes(errorHtml).AsMemory();
            _logger = logger;
            _reloadAuthorization = $"Bearer {reloadToken}";
        }

        public async Task GetRootAsync(HttpContext context) {
            context.Response.ContentType = "text/html";
            if (_indexHtmlBytes == null) {
                try {
                    await LoadInitialHtmlBytesAsync();
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to load initial HTML");
                    context.Response.StatusCode = 500;
                    await context.Response.BodyWriter.WriteAsync(_errorHtmlBytes);
                    return;
                }
            }
            
            await context.Response.BodyWriter.WriteAsync(_indexHtmlBytes!.Value);
        }

        private async Task LoadInitialHtmlBytesAsync() {
            await _initialLoadSemaphore.WaitAsync();
            try {
                if (_indexHtmlBytes != null)
                    return;

                await UpdateBytesAsync();
            }
            finally {
                _initialLoadSemaphore.Release();
            }
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
