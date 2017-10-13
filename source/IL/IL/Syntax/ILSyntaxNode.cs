using System.Text;
using JetBrains.Annotations;

namespace IL.Syntax {
    public abstract class ILSyntaxNode {
        public abstract void AppendToString([NotNull] StringBuilder builder);

        public override string ToString() {
            var builder = new StringBuilder();
            AppendToString(builder);
            return builder.ToString();
        }
    }
}