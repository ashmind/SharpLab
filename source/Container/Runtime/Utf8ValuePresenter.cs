using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SharpLab.Runtime.Internal;

namespace SharpLab.Container.Runtime {
    internal class Utf8ValuePresenter {
        public void Present(Span<byte> output, VariantValue value, ValuePresenterLimits limits, out int byteCount) {
            switch (value.Kind) {
                case VariantKind.Int32:
                    AppendNumber(output, value.AsInt32Unchecked(), out byteCount);
                    return;

                case VariantKind.Int64:
                    Utf8Formatter.TryFormat(value.AsInt64Unchecked(), output, out byteCount);
                    return;

                case VariantKind.Object:
                    AppendValue(output, value.AsObjectUnchecked(), depth: 1, limits, out byteCount);
                    return;

                default:
                    throw new NotSupportedException($"Unsupported variant kind: {value.Kind}");
            }
        }

        public int GetMaxOutputByteCount(ValuePresenterLimits limits) {
            if (limits.MaxDepth > 2)
                throw new NotSupportedException("Output length calculation can only be done for depth <= 2.");

            // maximum length is this (depending on item and sequence limits):
            // { { longestitem…, … }, { longestitem…, … }, … }

            const int ellipsis = Utf8Ellipsis.Length;
            const int bracesAndEllipsis = 2 // {␣
                 + ellipsis
                 + 2; // ␣}

            return (
                bracesAndEllipsis +
                + (limits.MaxValueLength - 1)
                + ellipsis
                + 2 // ,␣ (before last … inside)
                + 2 // ,␣ (before next item outside)
            ) * limits.MaxEnumerableItemCount
              + bracesAndEllipsis;
        }

        private void AppendValue<T>(Span<byte> output, T value, int depth, ValuePresenterLimits limits, out int byteCount) {
            if (value == null) {
                output[0] = (byte)'n';
                output[1] = (byte)'u';
                output[2] = (byte)'l';
                output[3] = (byte)'l';
                byteCount = 4;
                return;
            }

            switch (value) {
                case int i:
                    AppendNumber(output, i, out byteCount);
                    break;
                case IReadOnlyCollection<int> c:
                    AppendEnumerable(output, c, depth, limits, out byteCount);
                    break;
                case IReadOnlyCollection<char> c:
                    AppendEnumerable(output, c, depth, limits, out byteCount);
                    break;
                case ICollection c:
                    AppendEnumerable(output, c.Cast<object>(), depth, limits, out byteCount);
                    break;
                case DateTime date:
                    AppendString(output, date.ToString("dd.MM.yyyy HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture), limits, out byteCount);
                    break;
                case DateTimeOffset date:
                    AppendString(output, date.ToString("dd.MM.yyyy HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture), limits, out byteCount);
                    break;
                case IFormattable f:
                    AppendString(output, f.ToString(null, CultureInfo.InvariantCulture) ?? "", limits, out byteCount);
                    break;
                default:
                    AppendString(output, value.ToString() ?? "", limits, out byteCount);
                    break;
            }
        }

        private void AppendNumber(Span<byte> output, int number, out int byteCount) {
            Utf8Formatter.TryFormat(number, output, out byteCount);
        }

        private void AppendEnumerable<T>(
            Span<byte> output,
            IEnumerable<T> enumerable,
            int depth,
            ValuePresenterLimits limits,
            out int byteCount
        ) {
            if (depth > limits.MaxDepth) {
                output[0] = (byte)'{';
                byteCount = 1 + AppendEllipsis(output, 1);
                output[byteCount] = (byte)'}';
                byteCount += 1;
                return;
            }

            byteCount = Append(output, '{', ' ', 0);

            var index = 0;
            foreach (var item in enumerable) {
                if (index > 0)
                    byteCount += Append(output, ',', ' ', byteCount);

                if (index > limits.MaxEnumerableItemCount - 1) {
                    byteCount += AppendEllipsis(output, byteCount);
                    break;
                }

                AppendValue(
                    output.Slice(byteCount),
                    item, depth + 1,
                    limits.WithMaxEnumerableItemCount(1),
                    out var itemByteCount
                );
                byteCount += itemByteCount;

                index += 1;
            }

            byteCount += Append(output, ' ', '}', byteCount);
        }

        private void AppendString(Span<byte> output, string value, ValuePresenterLimits limits, out int byteCount) {
            if (value.Length > limits.MaxValueLength) {
                byteCount = limits.MaxValueLength - 1;
                CharBreakingUtf8Encoder.Encode(value.AsSpan().Slice(0, byteCount), output);
                byteCount += AppendEllipsis(output, byteCount);
                return;
            }

            byteCount = Encoding.UTF8.GetBytes(value, output);
        }

        private int Append(Span<byte> output, byte value1, byte value2, int offset) {
            output[offset] = value1;
            output[offset + 1] = value2;
            return 2;
        }

        private int Append(Span<byte> output, char value1, char value2, int offset) {
            return Append(output, (byte)value1, (byte)value2, offset);
        }

        private static int AppendEllipsis(Span<byte> output, int offset) {
            Utf8Ellipsis.CopyTo(output[offset..]);
            return Utf8Ellipsis.Length;
        }
    }
}