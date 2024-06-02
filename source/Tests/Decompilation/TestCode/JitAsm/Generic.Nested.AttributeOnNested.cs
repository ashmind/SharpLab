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

C+N`1[[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]].get_M()
    ; Failed to find JIT output. This might appear more frequently than before due to a library update.
    ; Please monitor https://github.com/ashmind/SharpLab/issues/1334 for progress.

*/