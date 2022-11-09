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

*/