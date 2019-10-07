using System;
using System.Collections.Generic;

namespace SharpLab.Runtime.Internal {
    public static class MemoryGraphArgumentNames {
        private static string[]? _next;
        private static int _nextAddIndex;

        public static void AllocateNext(int count) {
            _next = new string[count];
            _nextAddIndex = 0;
        }

        public static void AddToNext(string value) {
            _next![_nextAddIndex] = value;
            _nextAddIndex += 1;
        }

        public static IReadOnlyList<string> Collect() {
            var list = _next;
            _next = null;
            return list ?? Array.Empty<string>();
        }
    }
}
