using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Decompilation.AstOnly {
    public interface IAstTarget {
        Task<object?> GetAstAsync(IWorkSession session, CancellationToken cancellationToken);
        void SerializeAst(object ast, IFastJsonWriter writer, IWorkSession session);

        IReadOnlyCollection<string> SupportedLanguageNames { get; }
    }
}