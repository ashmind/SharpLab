using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace IL.Syntax {
    public abstract class DeclarationNode : ILSyntaxNode {
        protected DeclarationNode([NotNull] ISet<DeclarationModifier> modifiers) {
            Modifiers = modifiers;
        }

        public ISet<DeclarationModifier> Modifiers { get; }

        protected void AppendModifiersToString(StringBuilder builder) {
            var first = true;
            foreach (var modifier in Modifiers) {
                if (!first) {
                    builder.Append(" ");
                }
                else {
                    first = false;
                }
                builder.Append(modifier.Text);
            }
        }
    }

    public abstract class DeclarationNode<TName> : DeclarationNode {
        protected DeclarationNode([NotNull] TName name, [NotNull] ISet<DeclarationModifier> modifiers) : base(modifiers) {
            Name = name;
        }

        [NotNull] public TName Name { get; set; }
    }
}
