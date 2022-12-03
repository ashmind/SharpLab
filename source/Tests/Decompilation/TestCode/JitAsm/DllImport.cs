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

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+8], edx
    L0003: ret

*/