using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MirrorSharp.Advanced.EarlyAccess;

namespace SharpLab.Server.MirrorSharp.Guards {
    public class CSharpCompilationGuard : IRoslynCompilationGuard<CSharpCompilation> {
        public void ValidateCompilation(CSharpCompilation compilation) {
            foreach (var tree in compilation.SyntaxTrees) {
                if (!tree.TryGetRoot(out var root))
                    throw new InvalidOperationException();

                foreach (var qualified in ShallowDescendantsOfType<QualifiedNameSyntax>(root)) {
                    if (qualified is { Left: QualifiedNameSyntax { Left: QualifiedNameSyntax { Left: QualifiedNameSyntax _ } } })
                        throw new RoslynCompilationGuardException("Reference exceeded type nesting limit: " + qualified);
                }

                foreach (var generic in ShallowDescendantsOfType<TypeParameterListSyntax>(root)) {
                    if (generic.Parameters.Count > 4)
                        throw new RoslynCompilationGuardException("Generic parameter list exceeded size limit: " + generic);
                }

                foreach (var generic in ShallowDescendantsOfType<TypeArgumentListSyntax>(root)) {
                    if (GetTotalGenericArgumentCount(generic) > 5)
                        throw new RoslynCompilationGuardException("Generic argument list exceeded size limit: " + generic);
                }

                foreach (var type in ShallowDescendantsOfType<TypeDeclarationSyntax>(root)) {
                    EnsureNoRecursiveGenericPointerAttributes(type);
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

        private void EnsureNoRecursiveGenericPointerAttributes(TypeDeclarationSyntax type) {
            if (type.TypeParameterList == null)
                return;

            foreach (var attribute in ShallowDescendantsOfType<AttributeSyntax>(type)) {
                foreach (var typeArgumentList in ShallowDescendantsOfType<TypeArgumentListSyntax>(attribute)) {
                    foreach (var functionPointer in ShallowDescendantsOfType<FunctionPointerTypeSyntax>(typeArgumentList)) {
                        throw new RoslynCompilationGuardException("Specific use of pointer type in generics is not allowed due to high chance of application failure (see https://github.com/dotnet/roslyn/issues/65594): " + functionPointer);
                    }
                }
            }
        }

        private IEnumerable<TSyntaxNode> ShallowDescendantsOfType<TSyntaxNode>(SyntaxNode node)
            where TSyntaxNode : SyntaxNode {
            return node.DescendantNodes(static n => n is not TSyntaxNode).OfType<TSyntaxNode>();
        }
    }
}
