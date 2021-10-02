using SharpLab.Runtime;

[JitGeneric(typeof(int))]
[JitGeneric(typeof(string))]
static class C<T> {
    static class N {
        static T M() => default(T);
    }
}

/* asm

; Core CLR <IGNORE> on amd64

C`1+N[[System.Int32, System.Private.CoreLib]].M()
    L0000: xor eax, eax
    L0002: ret

C`1+N[[System.__Canon, System.Private.CoreLib]].M()
    L0000: xor eax, eax
    L0002: ret

*/