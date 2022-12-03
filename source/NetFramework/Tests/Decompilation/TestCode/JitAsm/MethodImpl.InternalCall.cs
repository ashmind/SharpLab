using System.Runtime.CompilerServices;

public static class C {
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void M();
}

/* asm

; Core CLR <IGNORE> on x64

C.M()
    ; Cannot produce JIT assembly for an internal call method.

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+8], edx
    L0003: ret

*/