using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace IL.Syntax {
    public class CompilationUnitNode : ILSyntaxNode {
        public CompilationUnitNode([NotNull, ItemNotNull] IReadOnlyList<DeclarationNode> declarations) {
            Declarations = declarations;
        }

        [NotNull, ItemNotNull] public IReadOnlyList<DeclarationNode> Declarations { get; }

        public override void AppendToString([NotNull] StringBuilder builder) {
            var first = true;
            foreach (var declaration in Declarations) {
                if (!first) {
                    builder.AppendLine();
                }
                else {
                    first = false;
                }
                declaration.AppendToString(builder);
            }
        }
    }
}
