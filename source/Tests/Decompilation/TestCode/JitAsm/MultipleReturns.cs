static class C {
    static int M(bool x) {
        return x ? 1 : 2;
    }
}

/* asm

; Core CLR <IGNORE> on x64

C.M(Boolean)
    L0000: test cl, cl
    L0002: jne short L000a
    L0004: mov eax, 2
    L0009: ret
    L000a: mov eax, 1
    L000f: ret

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+8], edx
    L0003: ret

*/