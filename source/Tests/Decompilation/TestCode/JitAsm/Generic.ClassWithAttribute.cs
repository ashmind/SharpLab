using SharpLab.Runtime;

[JitGeneric(typeof(int))]
[JitGeneric(typeof(decimal))]
[JitGeneric(typeof(string))]
static class C<T> {
    static T M() {
        return default(T);
    }
}

/* asm

; Core CLR <IGNORE> on x64

C`1[[System.Int32, System.Private.CoreLib]].M()
    L0000: xor eax, eax
    L0002: ret

C`1[[System.Decimal, System.Private.CoreLib]].M()
    L0000: vzeroupper
    L0003: vxorps xmm0, xmm0, xmm0
    L0007: vmovdqu [rcx], xmm0
    L000b: mov rax, rcx
    L000e: ret

C`1[[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]].M()
    ; Failed to find JIT output. This might appear more frequently than before due to a library update.
    ; Please monitor https://github.com/ashmind/SharpLab/issues/1334 for progress.

*/