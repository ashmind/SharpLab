using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharpLab.Server.Common;

namespace SharpLab.WebApp.Server.Assets {
    // Not using controller for this to avoid per-request allocations on a hot path
    public class IndexHtmlEndpoints {
        private readonly IIndexHtmlProvider _provider;
        private readonly CancellationToken _applicationStopping;
        private readonly string _reloadAuthorization;
        private readonly ILogger<IndexHtmlEndpoints> _logger;

        private int _indexHtmlStatusCode;
        private ReadOnlyMemory<byte> _indexHtmlBytes;

        public IndexHtmlEndpoints(
            IIndexHtmlProvider provider,
            string reloadToken,
            IHostApplicationLifetime applicationLifetime,
            ILogger<IndexHtmlEndpoints> logger
        ) {
            _provider = provider;
            _applicationStopping = applicationLifetime.ApplicationStopping;
            _reloadAuthorization = $"Bearer {reloadToken}";
            _logger = logger;
        }

        public async Task StartAsync() {
            var started = await TryStartAsync();
            if (!started) {
                _indexHtmlStatusCode = (int)HttpStatusCode.FailedDependency;
                _indexHtmlBytes = Encoding.UTF8.GetBytes("<!DOCTYPE html!><html><head><title></title></head><body>Application startup failed: could not load index.html from the assets storage.</body></html>");
                // Edge case, so not using hosted service or similar
                ScheduleStartRetry();
            }
        }

        private void ScheduleStartRetry() {
            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => {
                var remainingRetryCount = 30;
                bool started;
                do {
                    var delay = remainingRetryCount switch {
                        > 25 => TimeSpan.FromSeconds(10),
                        > 20 => TimeSpan.FromSeconds(30),
                        _ => TimeSpan.FromMinutes(1)
                    };
                    await Task.Delay(delay, _applicationStopping);
                    started = await TryStartAsync();
                    remainingRetryCount -= 1;
                } while (!started && remainingRetryCount >= 1);
            }, _applicationStopping);
            #pragma warning restore CS4014
        }

        private async Task<bool> TryStartAsync() {
            try {
                await UpdateBytesAsync(_applicationStopping);
                _indexHtmlStatusCode = (int)HttpStatusCode.OK;
                return true;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to load initial index.html. Retrying...");
                return false;
            }
        }

        public ValueTask GetRootAsync(HttpContext context) {
            context.Response.ContentType = "text/html";
            context.Response.StatusCode = _indexHtmlStatusCode;
            return context.Response.BodyWriter.WriteAsync(_indexHtmlBytes).AsUntypedValueTask();
        }

        public async Task PostReloadAsync(HttpContext context) {
            if (context.Request.Headers["Authorization"] != _reloadAuthorization) {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            await UpdateBytesAsync(context.RequestAborted);
            context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            return;
        }

        private async Task UpdateBytesAsync(CancellationToken cancellationToken) {
            _indexHtmlBytes = Encoding.UTF8.GetBytes(await _provider.GetIndexHtmlContentAsync(cancellationToken)).AsMemory();
        }
    }
}
