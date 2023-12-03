static class C {
    static int M(bool x) {
        return x ? 1 : 2;
    }
}

/* asm

; Desktop CLR <IGNORE> on amd64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+0x8], edx
    L0003: ret

C.M(Boolean)
    L0000: test cl, cl
    L0002: jnz L000a
    L0004: mov eax, 0x2
    L0009: ret
    L000a: mov eax, 0x1
    L000f: ret

*/