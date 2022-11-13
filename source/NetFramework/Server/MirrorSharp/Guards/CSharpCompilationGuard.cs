using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MirrorSharp.Advanced.EarlyAccess;

namespace SharpLab.Server.MirrorSharp.Guards {
    public class CSharpCompilationGuard : IRoslynCompilationGuard<CSharpCompilation> {
        public void ValidateCompilation(CSharpCompilation compilation) {
            foreach (var tree in compilation.SyntaxTrees) {
                if (!tree.TryGetRoot(out var root))
                    throw new InvalidOperationException();

                foreach (var qualified in root.DescendantNodes(n => !(n is QualifiedNameSyntax)).OfType<QualifiedNameSyntax>()) {
                    if (qualified is { Left: QualifiedNameSyntax { Left: QualifiedNameSyntax { Left: QualifiedNameSyntax _ } } })
                        throw new RoslynCompilationGuardException("Reference exceeded type nesting limit: " + qualified);
                }

                foreach (var generic in root.DescendantNodes(static n => n is not TypeParameterListSyntax).OfType<TypeParameterListSyntax>()) {
                    if (generic.Parameters.Count > 4)
                        throw new RoslynCompilationGuardException("Generic parameter list exceeded size limit: " + generic);
                }

                foreach (var generic in root.DescendantNodes(static n => n is not TypeArgumentListSyntax).OfType<TypeArgumentListSyntax>()) {
                    if (GetTotalGenericArgumentCount(generic) > 4)
                        throw new RoslynCompilationGuardException("Generic argument list exceeded size limit: " + generic);
                }
            }
        }

        private int GetTotalGenericArgumentCount(TypeArgumentListSyntax generic) {
            var count = 0;
            foreach (var subgeneric in generic.DescendantNodesAndSelf().OfType<TypeArgumentListSyntax>()) {
                count += subgeneric.Arguments.Count;
            }
            return count;
        }
    }
}
