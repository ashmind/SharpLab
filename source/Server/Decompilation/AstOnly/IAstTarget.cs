using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Decompilation.AstOnly {
    public interface IAstTarget {
        [NotNull, ItemCanBeNull] Task<object> GetAstAsync([NotNull] IWorkSession session, CancellationToken cancellationToken);
        void SerializeAst([NotNull] object ast, [NotNull] IFastJsonWriter writer);

        [NotNull, ItemNotNull] IReadOnlyCollection<string> SupportedLanguageNames { get; }
    }
}