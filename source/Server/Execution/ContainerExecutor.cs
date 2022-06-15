using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;
using SharpLab.Server.Execution.Container;
using SharpLab.Server.Execution.Internal;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Execution {
    public class ContainerExecutor : IContainerExecutor {
        private readonly IAssemblyStreamRewriterComposer _rewriterComposer;
        private readonly IContainerClient _client;

        public ContainerExecutor(
            IAssemblyStreamRewriterComposer rewriterComposer,
            IContainerClient client
        ) {
            _rewriterComposer = rewriterComposer;
            _client = client;
        }

        public async Task<ContainerExecutionResult> ExecuteAsync(CompilationStreamPair streams, IWorkSession session, CancellationToken cancellationToken) {
            Argument.NotNull(nameof(streams), streams);
            Argument.NotNull(nameof(session), session);

            using var rewritten = _rewriterComposer.Rewrite(streams, session);

            var includePerformance = session.ShouldReportPerformance();
            var executeStopwatch = includePerformance ? Stopwatch.StartNew() : null;
            var result = await _client.ExecuteAsync(session.GetSessionId(), rewritten.Stream, includePerformance, cancellationToken);
            
            if (rewritten.ElapsedTime != null && executeStopwatch != null) {
                // TODO: Prettify
                // output += $"\n  REWRITERS: {rewriteStopwatch.ElapsedMilliseconds,17}ms\n  CONTAINER EXECUTOR: {executeStopwatch.ElapsedMilliseconds,8}ms";
            }
            return result;
        }
    }
}