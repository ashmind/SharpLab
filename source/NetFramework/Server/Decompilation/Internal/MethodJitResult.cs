using System;

namespace SharpLab.Server.Decompilation.Internal {
    [Serializable]
    public struct MethodJitResult {
        public MethodJitResult(RuntimeMethodHandle handle, MethodJitStatus status) {
            Handle = handle.Value;
            Pointer = GetIsSuccess(status)
                    ? handle.GetFunctionPointer()
                    : (IntPtr?)null;
            Status = status;
        }

        public IntPtr Handle { get; }
        public IntPtr? Pointer { get; }
        public MethodJitStatus Status { get; }

        public bool IsSuccess => GetIsSuccess(Status);
        private static bool GetIsSuccess(MethodJitStatus status) {
            return status == MethodJitStatus.Success
                || status == MethodJitStatus.SuccessGeneric;
        }
    }
}