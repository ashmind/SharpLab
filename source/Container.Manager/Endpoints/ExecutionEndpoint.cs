using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SharpLab.Container.Manager.Internal;

namespace SharpLab.Container.Manager.Endpoints {
    // Not using controller for this to avoid per-request allocations on a hot path
    public class ExecutionEndpoint {
        private readonly ExecutionManager _executionManager;
        private readonly ILogger<ExecutionEndpoint> _logger;
        private readonly string _requiredBearerAuthorization;
        private readonly ConcurrentDictionary<string, byte> _activeSessionRequests = new();

        public ExecutionEndpoint(ExecutionManager executionManager, ExecutionEndpointSettings settings, ILogger<ExecutionEndpoint> logger) {
            _executionManager = executionManager;
            _logger = logger;
            _requiredBearerAuthorization = "Bearer " + settings.RequiredAuthorizationToken;
        }

        public async Task ExecuteAsync(HttpContext context) {
            var authorization = context.Request.Headers["Authorization"];
            if (authorization.Count != 1 || authorization[0] != _requiredBearerAuthorization) {
                context.Response.StatusCode = 401;
                context.Response.Headers["WWW-Authenticate"] = "Bearer";
                return;
            }

            var sessionId = context.Request.Headers["SL-Session-Id"][0]!;
            using var sessionLogScope = _logger.IsEnabled(LogLevel.Debug) ? _logger.BeginScope(("SessionId", sessionId)) : null;
            var includePerformance = context.Request.Headers["SL-Debug-Performance"].Count > 0;
            var contentLength = (int)context.Request.Headers.ContentLength!;

            _logger.LogDebug("Starting Execute");

            var stopwatch = includePerformance ? Stopwatch.StartNew() : null;

            byte[]? bodyBytes = null;
            byte[]? outputBuffer = null;
            try {
                if (!_activeSessionRequests.TryAdd(sessionId, 0))
                    await WriteErrorResponseAsync(context, new Exception($"Attempted parallel access to session {sessionId}."));

                bodyBytes = ArrayPool<byte>.Shared.Rent(contentLength);
                outputBuffer = ArrayPool<byte>.Shared.Rent(10240);

                var memoryStream = new MemoryStream(bodyBytes);
                await context.Request.Body.CopyToAsync(memoryStream);

                using var requestExecutionCancellation = CancellationFactory.RequestExecution(context);
                ExecutionOutputResult result;
                try {
                    result = await _executionManager.ExecuteAsync(sessionId, bodyBytes, outputBuffer, includePerformance, requestExecutionCancellation.Token);
                }
                catch (Exception ex) {
                    await WriteErrorResponseAsync(context, ex);
                    return;
                }

                try {
                    context.Response.StatusCode = 200;
                    if (!result.IsSuccess)
                        context.Response.Headers.Append("SL-Container-Output-Failed", "true");
                    await context.Response.BodyWriter.WriteAsync(result.Output, context.RequestAborted);
                    if (!result.IsSuccess)
                        await context.Response.BodyWriter.WriteAsync(result.FailureMessage, context.RequestAborted);

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
                _activeSessionRequests.TryRemove(sessionId, out var _);
                if (bodyBytes != null)
                    ArrayPool<byte>.Shared.Return(bodyBytes);
                if (outputBuffer != null)
                    ArrayPool<byte>.Shared.Return(outputBuffer);
                _logger.LogDebug("Completed Execute");
            }
        }

        private Task WriteErrorResponseAsync(HttpContext context, Exception exception) {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/vnd.sharplab.error+plain";
            _logger.LogError(exception, "Execution endpoint error");
            return context.Response.WriteAsync(exception.ToString(), context.RequestAborted);
        }
    }
}
