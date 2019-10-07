using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLab.Server.Explanation.Internal {
    public interface ISyntaxExplanationProvider {
        ValueTask<IReadOnlyCollection<SyntaxExplanation>> GetExplanationsAsync(CancellationToken cancellationToken);
    }
}
