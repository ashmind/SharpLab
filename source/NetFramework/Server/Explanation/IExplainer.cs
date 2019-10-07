using System.Threading;
using System.Threading.Tasks;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Explanation {
    public interface IExplainer {
        Task<ExplanationResult> ExplainAsync(object ast, IWorkSession session, CancellationToken cancellationToken);
        void Serialize(ExplanationResult result, IFastJsonWriter writer);
    }
}