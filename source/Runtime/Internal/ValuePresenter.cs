using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLab.Runtime.Internal {
    internal static class ValuePresenter {
        public static StringBuilder ToStringBuilder<T>(T value, int? maxEnumerableItemCount = null, int? maxValueLength = null) {
            var builder = new StringBuilder();
            AppendTo(builder, value, maxEnumerableItemCount, maxValueLength);
            return builder;
        }

        public static void AppendTo<T>(StringBuilder builder, T value, int? maxEnumerableItemCount = null, int? maxValueLength = null) {
            if (value == null) {
                builder.Append("null");
                return;
            }

            switch (value) {
                case ICollection<int> c:
                    AppendEnumerableTo(builder, c, maxEnumerableItemCount, maxValueLength);
                    break;
                case ICollection c:
                    AppendEnumerableTo(builder, c.Cast<object>(), maxEnumerableItemCount, maxValueLength);
                    break;
                default:
                    AppendStringTo(builder, value.ToString(), maxValueLength);
                    break;
            }
        }

        private static void AppendEnumerableTo<T>(StringBuilder builder, IEnumerable<T> enumerable, int? maxItemCount, int? maxValueLength) {
            builder.Append("{ ");
            var index = 0;
            foreach (var item in enumerable) {
                if (index > 0)
                    builder.Append(", ");

                if (index > maxItemCount) {
                    builder.Append("…");
                    break;
                }

                AppendTo(builder, item, maxItemCount, maxValueLength);
                index += 1;
            }
            builder.Append(" }");
        }

        public static void AppendStringTo(StringBuilder builder, string value, int? maxLength) {
            if (maxLength == null || value.Length <= maxLength) {
                builder.Append(value);
            }
            else {
                builder.Append(value, 0, maxLength.Value - 1);
                builder.Append("…");
            }
        }
    }
}