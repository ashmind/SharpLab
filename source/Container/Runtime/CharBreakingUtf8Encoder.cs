using System;
using System.Text;

namespace SharpLab.Container.Runtime {
    // NOT thread-safe
    public static class CharBreakingUtf8Encoder {
        private static readonly Encoder _utf8Encoder = Encoding.UTF8.GetEncoder();

        public static void Encode(ReadOnlySpan<char> chars, Span<byte> bytes) {
            _utf8Encoder.Convert(chars, bytes, flush: true, out _, out _, out _);
            _utf8Encoder.Reset();
        }
    }
}
