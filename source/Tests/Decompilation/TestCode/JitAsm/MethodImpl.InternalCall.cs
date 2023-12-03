using System.Runtime.CompilerServices;

public static class C {
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void M();
}

/* asm

; Core CLR <IGNORE> on x64

C.M()
    ; Cannot produce JIT assembly for an internal call method.

*/