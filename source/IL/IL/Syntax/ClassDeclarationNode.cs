using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace IL.Syntax {
    public class ClassDeclarationNode : DeclarationNode {
        public ClassDeclarationNode([NotNull] string name, [NotNull] ISet<DeclarationModifier> modifiers) : base(name, modifiers) {
        }

        public override void AppendToString(StringBuilder builder) {
            builder.Append(".class ");
            if (Modifiers.Any()) {
                AppendModifiersToString(builder);
                builder.Append(" ");
            }
            AppendNameToString(builder);
            builder.Append(" {");
            builder.Append("}");
        }
    }
}
