using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Common.Languages
{
    public static class RoslynAdapterHelper {
        [CanBeNull]
        public static SyntaxNode? FindSyntaxNodeInSession([NotNull] IWorkSession session, int line, int column) {
            if (!session.Roslyn.Project.TryGetCompilation(out var compilation))
                return null;
            var syntaxTree = compilation.SyntaxTrees.First();
            if (!syntaxTree.TryGetText(out var text))
                return null;
            var textLine = text.Lines[line - 1];
            var span = new TextSpan(textLine.Start + column - 1, 0);
            return syntaxTree.GetRoot().FindNode(span);
        }
    }
}
