using System.Runtime.InteropServices;

public static class NativeMethods
{
    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();
}

/* asm

; Desktop CLR <IGNORE> on x86

NativeMethods.GetLastError()
    ; Cannot produce JIT assembly for a P/Invoke method.

*/