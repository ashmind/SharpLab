using SharpLab.Runtime;

[JitGeneric(typeof(int))]
[JitGeneric(typeof(string))]
static class C<T> {
    static class N {
        static T M() => default(T);
    }
}

/* asm

; Desktop CLR <IGNORE> on amd64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+0x8], edx
    L0003: ret

C`1+N[[System.Int32, mscorlib]].M()
    L0000: xor eax, eax
    L0002: ret

C`1+N[[System.__Canon, mscorlib]].M()
    L0000: xor eax, eax
    L0002: ret

*/