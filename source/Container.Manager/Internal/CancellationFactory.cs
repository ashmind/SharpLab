using System.Threading;
using Microsoft.AspNetCore.Http;

namespace SharpLab.Container.Manager.Internal {
    public static class CancellationFactory {
        public static CancellationTokenSource RequestExecution(HttpContext context)
            => CancelTokenAfter(context.RequestAborted, 25000);

        public static CancellationTokenSource ContainerAllocation(CancellationToken linkedToken)
            => CancelTokenAfter(linkedToken, 5000);

        public static CancellationTokenSource ContainerExecution(CancellationToken linkedToken)
            => CancelTokenAfter(linkedToken, 10000);

        public static CancellationTokenSource ContainerWarmup(CancellationToken linkedToken)
            => CancelTokenAfter(linkedToken, 30000);

        private static CancellationTokenSource CancelTokenAfter(CancellationToken linkedToken, int cancelDelayMilliseconds) {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(linkedToken);
            try {
                cancellationTokenSource.CancelAfter(cancelDelayMilliseconds);
            }
            catch {
                cancellationTokenSource.Dispose();
                throw;
            }
            return cancellationTokenSource;
        }
    }
}
