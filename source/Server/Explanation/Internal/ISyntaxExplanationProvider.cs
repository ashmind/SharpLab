using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpLab.Server.Explanation.Internal {
    public interface ISyntaxExplanationProvider {
        ValueTask<IReadOnlyDictionary<SyntaxKind, SyntaxExplanation>> GetExplanationsAsync(CancellationToken cancellationToken);
    }
}
