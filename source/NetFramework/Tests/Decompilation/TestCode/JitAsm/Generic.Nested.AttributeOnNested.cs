using SharpLab.Runtime;

static class C {
    [JitGeneric(typeof(int))]
    [JitGeneric(typeof(string))]
    static class N<T> {
        static T M => default(T);
    }
}

/* asm

; Desktop CLR <IGNORE> on amd64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+0x8], edx
    L0003: ret

C+N`1[[System.Int32, mscorlib]].get_M()
    L0000: xor eax, eax
    L0002: ret

C+N`1[[System.__Canon, mscorlib]].get_M()
    L0000: xor eax, eax
    L0002: ret

*/