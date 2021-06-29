using System;

namespace SharpLab.Runtime {
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public class JitGenericAttribute : Attribute {
        public JitGenericAttribute(params Type[] argumentTypes) {
            ArgumentTypes = argumentTypes;
        }

        public Type[] ArgumentTypes { get; }
    }
}
