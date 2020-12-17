using System;
using System.Linq;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using MirrorSharp.Advanced.EarlyAccess;
using SharpLab.Server.Compilation.Guards;

namespace SharpLab.Server.Compilation.Internal.Guards {
    public class VisualBasicGuard : IRoslynGuardInternal<VisualBasicCompilation> {
        public void ValidateCompilation(VisualBasicCompilation compilation) {
            foreach (var tree in compilation.SyntaxTrees) {
                if (!tree.TryGetRoot(out var root))
                    throw new InvalidOperationException();

                foreach (var qualified in root.DescendantNodes(n => !(n is QualifiedNameSyntax)).OfType<QualifiedNameSyntax>()) {
                    if (qualified is { Left: QualifiedNameSyntax { Left: QualifiedNameSyntax { Left: QualifiedNameSyntax _ } } })
                        throw new RoslynGuardException("Reference exceeded type nesting limit: " + qualified);
                }

                foreach (var generic in root.DescendantNodes().OfType<TypeParameterListSyntax>()) {
                    if (generic.Parameters.Count > 4)
                        throw new RoslynGuardException("Generic parameter list exceeded size limit: " + generic);
                }

            }
        }
    }
}
