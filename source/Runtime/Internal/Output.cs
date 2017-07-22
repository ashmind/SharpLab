using System.Collections.Generic;

namespace SharpLab.Runtime.Internal {
    public static class Output {
        private static readonly List<object> _stream = new List<object>();
        public static IReadOnlyList<object> Stream => _stream;

        public static void Write(InspectionResult data) {
            _stream.Add(data);
        }

        public static void Write(string value) {
            _stream.Add(value);
        }
    }
}