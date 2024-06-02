using SharpLab.Runtime;
static class C {
    [JitGeneric(typeof(int))]
    [JitGeneric(typeof(decimal))]
    [JitGeneric(typeof(string))]
    static T M<T>() {
        return default(T);
    }
}

/* asm

; Core CLR <IGNORE> on x64

C.M[[System.Int32, System.Private.CoreLib]]()
    L0000: xor eax, eax
    L0002: ret

C.M[[System.Decimal, System.Private.CoreLib]]()
    L0000: vzeroupper
    L0003: vxorps xmm0, xmm0, xmm0
    L0007: vmovdqu [rcx], xmm0
    L000b: mov rax, rcx
    L000e: ret

C.M[[System.String, System.Private.CoreLib]]()
    ; Failed to find JIT output. This might appear more frequently than before due to a library update.
    ; Please monitor https://github.com/ashmind/SharpLab/issues/1334 for progress.

*/