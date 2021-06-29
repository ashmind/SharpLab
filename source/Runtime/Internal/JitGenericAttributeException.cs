using System;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class JitGenericAttributeException : Exception {
        public JitGenericAttributeException() {}
        public JitGenericAttributeException(string message) : base(message) {}
        public JitGenericAttributeException(string message, Exception inner) : base(message, inner) {}
    }
}