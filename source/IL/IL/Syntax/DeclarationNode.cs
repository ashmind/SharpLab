using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace IL.Syntax {
    public abstract class DeclarationNode : ILSyntaxNode {
        public DeclarationNode([NotNull] string name, [NotNull] ISet<DeclarationModifier> modifiers) {
            Name = name;
            Modifiers = modifiers;
        }

        [NotNull] public string Name { get; set; }
        public ISet<DeclarationModifier> Modifiers { get; }

        protected void AppendNameToString(StringBuilder builder) {
            if (ShouldEscapeName()) {
                builder.Append("'").Append(Name).Append("'");
            }
            else {
                builder.Append(Name);
            }
        }

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

        private bool ShouldEscapeName() {
            foreach (var @char in Name) {
                if (!char.IsLetterOrDigit(@char) && @char != '.')
                    return true;
            }
            return false;
        }
    }
}
