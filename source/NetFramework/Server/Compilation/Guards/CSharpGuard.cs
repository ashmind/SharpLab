using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MirrorSharp.Advanced.EarlyAccess;
using SharpLab.Server.Compilation.Guards;

namespace SharpLab.Server.Compilation.Internal.Guards {
    public class CSharpGuard : IRoslynGuardInternal<CSharpCompilation> {
        public void ValidateCompilation(CSharpCompilation compilation) {
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
