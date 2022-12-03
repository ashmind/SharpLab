using SharpLab.Runtime;

[JitGeneric(typeof(int))]
[JitGeneric(typeof(string))]
static class C<T> {
    [JitGeneric(typeof(int))]
    [JitGeneric(typeof(string))]
    static class N<U> {
        static T M(U u) => default(T);
    }
}

/* asm

; Desktop CLR <IGNORE> on amd64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+0x8], edx
    L0003: ret

C`1+N`1[[System.Int32, mscorlib],[System.Int32, mscorlib]].M(Int32)
    L0000: xor eax, eax
    L0002: ret

C`1+N`1[[System.Int32, mscorlib],[System.__Canon, mscorlib]].M(System.__Canon)
    L0000: xor eax, eax
    L0002: ret

C`1+N`1[[System.__Canon, mscorlib],[System.Int32, mscorlib]].M(Int32)
    L0000: xor eax, eax
    L0002: ret

C`1+N`1[[System.__Canon, mscorlib],[System.__Canon, mscorlib]].M(System.__Canon)
    L0000: xor eax, eax
    L0002: ret

*/