using SharpLab.Runtime;

static class C {
    [JitGeneric(typeof(int))]
    [JitGeneric(typeof(string))]
    static class N<T> {
        static T M => default(T);
    }
}

/* asm

; Core CLR <IGNORE> on x64

C+N`1[[System.Int32, System.Private.CoreLib]].get_M()
    L0000: xor eax, eax
    L0002: ret

C+N`1[[System.__Canon, System.Private.CoreLib]].get_M()
    L0000: xor eax, eax
    L0002: ret

*/