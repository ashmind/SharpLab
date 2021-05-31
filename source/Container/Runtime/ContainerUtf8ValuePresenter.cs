using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SharpLab.Runtime.Internal;

namespace SharpLab.Container.Runtime {
    internal class ContainerUtf8ValuePresenter {
        public void Present(Span<byte> output, VariantValue value, ValuePresenterLimits limits, out int byteCount) {
            switch (value.Kind) {
                case VariantKind.Int32:
                    Utf8Formatter.TryFormat(value.AsInt32Unchecked(), output, out byteCount);
                    return;

                case VariantKind.Int64:
                    Utf8Formatter.TryFormat(value.AsInt64Unchecked(), output, out byteCount);
                    return;

                case VariantKind.Object:
                    AppendObject(output, value.AsObjectUnchecked(), depth: 1, limits, out byteCount);
                    return;

                default:
                    throw new NotSupportedException($"Unsupported variant kind: {value.Kind}");
            }
        }

        private void AppendValue<T>(Span<byte> output, T value, int depth, ValuePresenterLimits limits, out int byteCount) {
            switch (value) {
                case int i:
                    Utf8Formatter.TryFormat(i, output, out byteCount);
                    return;

                default:
                    AppendObject(output, value, depth, limits, out byteCount);
                    return;
            }
        }

        private void AppendObject(Span<byte> output, object? value, int depth, ValuePresenterLimits limits, out int byteCount) {
            if (value == null) {
                output[0] = (byte)'n';
                output[1] = (byte)'u';
                output[2] = (byte)'l';
                output[3] = (byte)'l';
                byteCount = 4;
                return;
            }

            if (depth > limits.MaxDepth) {
                byteCount = AppendEllipsis(output, 0);
                return;
            }

            switch (value) {
                case ICollection<int> c:
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

        private void AppendEnumerable<T>(Span<byte> output, IEnumerable<T> enumerable, int depth, ValuePresenterLimits limits, out int byteCount) {
            byteCount = Append(output, '{', ' ', 0);

            var index = 0;
            foreach (var item in enumerable) {
                if (index > 0)
                    byteCount += Append(output, ',', ' ', byteCount);

                if (index > limits.MaxEnumerableItemCount) {
                    byteCount += AppendEllipsis(output, byteCount);
                    break;
                }

                AppendValue(output.Slice(byteCount), item, depth + 1, limits, out var itemByteCount);
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