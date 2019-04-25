using System;

namespace SharpLab.Server.Decompilation.Internal {
    [Serializable]
    public enum MethodJitStatus {
        Success,
        SuccessGeneric,
        IgnoredRuntime,
        IgnoredOpenGenericWithNoAttribute
    }
}