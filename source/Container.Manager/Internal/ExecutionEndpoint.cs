using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SharpLab.Container.Manager.Internal {
    // Not using controller for this to avoid per-request allocations on a hot path
    public class ExecutionEndpoint {
        private readonly ExecutionHandler _handler;
        private readonly ExecutionEndpointSettings _settings;

        public ExecutionEndpoint(ExecutionHandler handler, ExecutionEndpointSettings settings) {
            _handler = handler;
            _settings = settings;
        }

        public async Task ExecuteAsync(HttpContext context) {
            var stopwatch = Stopwatch.StartNew();
            var authorization = context.Request.Headers["Authorization"];
            if (authorization.Count != 1 || authorization[0] != _settings.RequiredAuthorization) {
                context.Response.StatusCode = 401;
                return;
            }

            var sessionId = context.Request.Headers["Sl-Session-Id"][0]!;
            var memoryStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(memoryStream);

            context.Response.StatusCode = 200;
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            timeoutSource.CancelAfter(10000);
            try {
                var result = await _handler.ExecuteAsync(sessionId, memoryStream.ToArray(), timeoutSource.Token);

                var bytes = new byte[Encoding.UTF8.GetByteCount(result.Span)];
                Encoding.UTF8.GetBytes(result.Span, bytes);
                await context.Response.BodyWriter.WriteAsync(bytes, context.RequestAborted);
                await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes($"\n  [VM] CONTAINER MANAGER: {stopwatch.ElapsedMilliseconds,4}ms"), context.RequestAborted);
            }
            catch (Exception ex) {
                await context.Response.WriteAsync(ex.ToString(), context.RequestAborted);
            }
        }
    }
}
