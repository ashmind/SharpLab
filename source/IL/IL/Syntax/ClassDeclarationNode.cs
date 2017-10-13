using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace IL.Syntax {
    public class ClassDeclarationNode : DeclarationNode<MultipartIdentifier> {
        public ClassDeclarationNode(
            [NotNull] MultipartIdentifier name,
            [NotNull] ISet<DeclarationModifier> modifiers,
            [CanBeNull] TypeReferenceNode extendsType = null
        ) : base(name, modifiers) {
            ExtendsType = extendsType;
        }

        [CanBeNull] public TypeReferenceNode ExtendsType { get; }

        public override void AppendToString(StringBuilder builder) {
            builder.Append(".class ");
            if (Modifiers.Any()) {
                AppendModifiersToString(builder);
                builder.Append(" ");
            }
            AppendNameToString(builder, Name);

            if (ExtendsType != null) {
                builder.Append(" extends ");
                ExtendsType.AppendToString(builder);
            }

            builder.Append(" {");
            builder.Append("}");
        }
    }
}
