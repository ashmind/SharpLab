using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SharpLab.Container.Manager.Internal {
    public static class NativeMethods {
        [DllImport("kernel32.dll")]
        public static extern bool CancelIoEx(SafePipeHandle hFile, IntPtr lpOverlapped);
    }
}
