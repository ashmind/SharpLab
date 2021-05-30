using System;

namespace SharpLab.Container.Runtime {
    // Character '…' (used to truncate text)
    internal static class Utf8Ellipsis {
        public const int Length = 3;

        public static void CopyTo(Span<byte> span) {
            span[0] = 226;
            span[1] = 128;
            span[2] = 166;
        }
    }
}
