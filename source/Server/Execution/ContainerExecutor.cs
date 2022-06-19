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
            var includePerformance = session.ShouldReportPerformance();

            using var rewrittenStream = _rewriterComposer.Rewrite(streams, session);

            var executeStopwatch = includePerformance ? Stopwatch.StartNew() : null;
            var result = await _client.ExecuteAsync(session.GetSessionId(), rewrittenStream, includePerformance, cancellationToken);
            if (/*rewriteStopwatch != null &&*/ executeStopwatch != null) {
                // TODO: Prettify
                // output += $"\n  REWRITERS: {rewriteStopwatch.ElapsedMilliseconds,17}ms\n  CONTAINER EXECUTOR: {executeStopwatch.ElapsedMilliseconds,8}ms";
            }
            return result;
        }
    }
}