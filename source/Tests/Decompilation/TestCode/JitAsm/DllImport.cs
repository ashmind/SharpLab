using System.Runtime.InteropServices;

public static class NativeMethods
{
    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();
}

/* asm

; Core CLR <IGNORE> on x64

NativeMethods.GetLastError()
    ; Cannot produce JIT assembly for a P/Invoke method.

*/