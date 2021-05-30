using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SharpLab.Container.Manager.Internal;

namespace SharpLab.Container.Manager.Endpoints {
    // Not using controller for this to avoid per-request allocations on a hot path
    public class ExecutionEndpoint {
        private readonly ExecutionManager _executionManager;
        private readonly ExecutionEndpointSettings _settings;

        public ExecutionEndpoint(ExecutionManager executionManager, ExecutionEndpointSettings settings) {
            _executionManager = executionManager;
            _settings = settings;
        }

        public async Task ExecuteAsync(HttpContext context) {
            var authorization = context.Request.Headers["Authorization"];
            if (authorization.Count != 1 || authorization[0] != _settings.RequiredAuthorization) {
                context.Response.StatusCode = 401;
                return;
            }

            var sessionId = context.Request.Headers["Sl-Session-Id"][0]!;
            var includePerformance = context.Request.Headers["Sl-Debug-Performance"].Count > 0;
            var contentLength = (int)context.Request.Headers.ContentLength!;

            var stopwatch = includePerformance ? Stopwatch.StartNew() : null;

            byte[]? bodyBytes = null;
            byte[]? outputBuffer = null;
            try {
                bodyBytes = ArrayPool<byte>.Shared.Rent(contentLength);
                outputBuffer = ArrayPool<byte>.Shared.Rent(10240);

                var memoryStream = new MemoryStream(bodyBytes);
                await context.Request.Body.CopyToAsync(memoryStream);

                context.Response.StatusCode = 200;
                using var requestExecutionCancellation = CancellationFactory.RequestExecution(context);
                try {
                    var result = await _executionManager.ExecuteAsync(sessionId, bodyBytes, outputBuffer, includePerformance, requestExecutionCancellation.Token);

                    await context.Response.BodyWriter.WriteAsync(result.Output, context.RequestAborted);
                    if (!result.IsOutputReadSuccess)
                        await context.Response.BodyWriter.WriteAsync(result.OutputReadFailureMessage, context.RequestAborted);

                    if (stopwatch != null) {
                        // TODO: Prettify. Put into header?
                        await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes($"\n  [VM] CONTAINER MANAGER: {stopwatch.ElapsedMilliseconds,4}ms"), context.RequestAborted);
                    }
                }
                catch (Exception ex) {
                    await context.Response.WriteAsync(ex.ToString(), context.RequestAborted);
                }
            }
            finally {
                if (bodyBytes != null)
                    ArrayPool<byte>.Shared.Return(bodyBytes);
                if (outputBuffer != null)
                    ArrayPool<byte>.Shared.Return(outputBuffer);
            }
        }
    }
}
