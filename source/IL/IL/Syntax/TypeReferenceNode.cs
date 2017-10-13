using System.Text;
using JetBrains.Annotations;

namespace IL.Syntax {
    public class TypeReferenceNode : ILSyntaxNode {
        public TypeReferenceNode([NotNull] MultipartIdentifier name, [CanBeNull] MultipartIdentifier assemblyName = null) {
            Name = name;
            AssemblyName = assemblyName;
        }

        [NotNull] public MultipartIdentifier Name { get; }
        [CanBeNull] public MultipartIdentifier AssemblyName { get; }

        public override void AppendToString(StringBuilder builder) {
            if (AssemblyName != null) {
                builder.Append('[');
                AppendNameToString(builder, AssemblyName);
                builder.Append(']');
            }
            AppendNameToString(builder, Name);            
        }
    }
}