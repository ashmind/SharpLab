using System.Runtime.InteropServices;

public static class NativeMethods
{
    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();
}

/* asm

; Desktop CLR <IGNORE> on amd64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+0x8], edx
    L0003: ret

NativeMethods.GetLastError()
    ; Cannot produce JIT assembly for a P/Invoke method.

*/