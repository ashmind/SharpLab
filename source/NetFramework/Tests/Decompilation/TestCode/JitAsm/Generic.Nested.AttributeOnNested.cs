using SharpLab.Runtime;

static class C {
    [JitGeneric(typeof(int))]
    [JitGeneric(typeof(string))]
    static class N<T> {
        static T M => default(T);
    }
}

/* asm

; Desktop CLR <IGNORE> on x64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+0x8], edx
    L0003: ret

C+N`1[[System.Int32, mscorlib]].get_M()
    L0000: xor eax, eax
    L0002: ret

Unknown (0x<IGNORE>)
    ; Method signature was not found -- please report this issue.
    ; Failed to find JIT output. This might appear more frequently than before due to a library update.
    ; Please monitor https://github.com/ashmind/SharpLab/issues/1334 for progress.

*/