using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLab.Runtime.Internal {
    internal static class ValuePresenter {
        public static StringBuilder ToStringBuilder<T>(T value, ValuePresenterLimits limits = default) {
            var builder = new StringBuilder();
            AppendTo(builder, value, limits);
            return builder;
        }

        public static void AppendTo<T>(StringBuilder builder, T value, ValuePresenterLimits limits = default) {
            AppendTo(builder, value, depth: 1, limits);
        }

        public static void AppendTo<T>(StringBuilder builder, ReadOnlySpan<T> value, ValuePresenterLimits limits = default) {
            AppendSpanTo(builder, value, depth: 1, limits);
        }

        private static void AppendTo<T>(StringBuilder builder, T value, int depth, ValuePresenterLimits limits = default)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            if (depth > limits.MaxDepth)
            {
                builder.Append("…");
                return;
            }

            switch (value)
            {
                case ICollection<int> c:
                    AppendEnumerableTo(builder, c, depth, limits);
                    break;
                case ICollection c:
                    AppendEnumerableTo(builder, c.Cast<object>(), depth, limits);
                    break;
                default:
                    AppendStringTo(builder, value.ToString() ?? "", limits);
                    break;
            }
        }

        private static void AppendEnumerableTo<T>(StringBuilder builder, IEnumerable<T> enumerable, int depth, ValuePresenterLimits limits) {
            builder.Append("{ ");
            var index = 0;
            foreach (var item in enumerable) {
                if (index > 0)
                    builder.Append(", ");

                if (index > limits.MaxEnumerableItemCount) {
                    builder.Append("…");
                    break;
                }

                AppendTo(builder, item, depth + 1, limits);
                index += 1;
            }
            builder.Append(" }");
        }

        private static void AppendSpanTo<T>(StringBuilder builder, ReadOnlySpan<T> value, int depth, ValuePresenterLimits limits) {
            builder.Append("{ ");
            var index = 0;
            foreach (var item in value) {
                if (index > 0)
                    builder.Append(", ");

                if (index > limits.MaxEnumerableItemCount) {
                    builder.Append("…");
                    break;
                }

                AppendTo(builder, item, depth + 1, limits);
                index += 1;
            }
            builder.Append(" }");
        }

        public static void AppendStringTo(StringBuilder builder, string value, ValuePresenterLimits limits) {
            if (limits.MaxValueLength == null || value.Length <= limits.MaxValueLength) {
                builder.Append(value);
            }
            else {
                builder.Append(value, 0, limits.MaxValueLength.Value - 1);
                builder.Append("…");
            }
        }
    }
}