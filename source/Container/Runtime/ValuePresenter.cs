using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SharpLab.Runtime.Internal;

namespace SharpLab.Container.Runtime {
    internal class ValuePresenter : IValuePresenter {
        public StringBuilder ToStringBuilder<T>(T value, ValuePresenterLimits limits = default) {
            var builder = new StringBuilder();
            AppendTo(builder, value, limits);
            return builder;
        }

        public StringBuilder ToStringBuilder<T>(ReadOnlySpan<T> value, ValuePresenterLimits limits = default) {
            var builder = new StringBuilder();
            AppendTo(builder, value, limits);
            return builder;
        }

        public void AppendTo<T>(StringBuilder builder, T value, ValuePresenterLimits limits = default) {
            AppendTo(builder, value, depth: 1, limits);
        }

        public void AppendTo<T>(StringBuilder builder, ReadOnlySpan<T> value, ValuePresenterLimits limits = default) {
            AppendSpanTo(builder, value, depth: 1, limits);
        }

        private void AppendTo<T>(StringBuilder builder, T value, int depth, ValuePresenterLimits limits = default) {
            if (value == null) {
                builder.Append("null");
                return;
            }

            if (depth > limits.MaxDepth) {
                builder.Append("…");
                return;
            }

            switch (value) {
                case ICollection<int> c:
                    AppendEnumerableTo(builder, c, depth, limits);
                    break;
                case ICollection c:
                    AppendEnumerableTo(builder, c.Cast<object>(), depth, limits);
                    break;
                case DateTime date:
                    AppendStringTo(builder, date.ToString("dd.MM.yyyy HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture), limits);
                    break;
                case DateTimeOffset date:
                    AppendStringTo(builder, date.ToString("dd.MM.yyyy HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture), limits);
                    break;
                case IFormattable f:
                    AppendStringTo(builder, f.ToString(null, CultureInfo.InvariantCulture) ?? "", limits);
                    break;
                default:
                    AppendStringTo(builder, value.ToString() ?? "", limits);
                    break;
            }
        }

        public void AppendEnumerableTo<T>(StringBuilder builder, IEnumerable<T> enumerable, int depth, ValuePresenterLimits limits) {
            builder.Append("{ ");
            var index = 0;
            foreach (var item in enumerable) {
                if (index > 0)
                    builder.Append(", ");

                if (index > limits.MaxEnumerableItemCount) {
                    builder.Append('…');
                    break;
                }

                AppendTo(builder, item, depth + 1, limits);
                index += 1;
            }
            builder.Append(" }");
        }

        private void AppendSpanTo<T>(StringBuilder builder, ReadOnlySpan<T> value, int depth, ValuePresenterLimits limits) {
            builder.Append("{ ");
            var index = 0;
            foreach (var item in value) {
                if (index > 0)
                    builder.Append(", ");

                if (index > limits.MaxEnumerableItemCount) {
                    builder.Append('…');
                    break;
                }

                AppendTo(builder, item, depth + 1, limits);
                index += 1;
            }
            builder.Append(" }");
        }

        public void AppendStringTo(StringBuilder builder, string value, ValuePresenterLimits limits) {
            if (value.Length <= limits.MaxValueLength) {
                builder.Append(value);
            }
            else {
                builder.Append(value, 0, limits.MaxValueLength - 1);
                builder.Append('…');
            }
        }
    }
}