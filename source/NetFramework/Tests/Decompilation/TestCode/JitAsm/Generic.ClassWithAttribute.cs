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

; Desktop CLR <IGNORE> on amd64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+0x8], edx
    L0003: ret

C`1[[System.Int32, mscorlib]].M()
    L0000: xor eax, eax
    L0002: ret

C`1[[System.Decimal, mscorlib]].M()
    L0000: vzeroupper
    L0003: vxorps xmm0, xmm0, xmm0
    L0008: vmovdqu [rcx], xmm0
    L000d: mov rax, rcx
    L0010: ret

C`1[[System.__Canon, mscorlib]].M()
    L0000: xor eax, eax
    L0002: ret

*/