using System;
using System.Runtime.Serialization;

namespace TryRoslyn.Web.Internal {
    [Serializable]
    public class CompilationException : Exception {
        public CompilationException() {
        }

        public CompilationException(string message) : base(message) {
        }

        public CompilationException(string message, Exception inner) : base(message, inner) {
        }

        protected CompilationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {
        }
    }
}