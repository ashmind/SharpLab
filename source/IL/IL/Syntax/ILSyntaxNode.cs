using System.Text;
using JetBrains.Annotations;

namespace IL.Syntax {
    public abstract class ILSyntaxNode {
        protected void AppendNameToString(StringBuilder builder, MultipartIdentifier name) {
            var first = true;
            foreach (var part in name.Parts) {
                if (!first) {
                    builder.Append('.');
                }
                else {
                    first = false;
                }
                AppendNameToString(builder, part);
            }
        }

        protected void AppendNameToString(StringBuilder builder, string name) {
            if (ShouldEscapeName(name)) {
                builder.Append("'").Append(name).Append("'");
            }
            else {
                builder.Append(name);
            }
        }

        private bool ShouldEscapeName(string name) {
            foreach (var @char in name) {
                if (!char.IsLetterOrDigit(@char) && @char != '.')
                    return true;
            }
            return false;
        }

        public abstract void AppendToString([NotNull] StringBuilder builder);

        public override string ToString() {
            var builder = new StringBuilder();
            AppendToString(builder);
            return builder.ToString();
        }
    }
}