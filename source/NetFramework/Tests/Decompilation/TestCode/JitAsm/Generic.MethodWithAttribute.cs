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

; Desktop CLR <IGNORE> on x64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+0x8], edx
    L0003: ret

C.M[[System.Int32, mscorlib]]()
    L0000: xor eax, eax
    L0002: ret

C.M[[System.Decimal, mscorlib]]()
    L0000: vzeroupper
    L0003: vxorps xmm0, xmm0, xmm0
    L0008: vmovdqu [rcx], xmm0
    L000d: mov rax, rcx
    L0010: ret

Unknown (0x<IGNORE>)
    ; Method signature was not found -- please report this issue.
    ; Failed to find JIT output. This might appear more frequently than before due to a library update.
    ; Please monitor https://github.com/ashmind/SharpLab/issues/1334 for progress.

*/