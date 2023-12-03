static class C {
    static int M(bool x) {
        return x ? 1 : 2;
    }
}

/* asm

; Core CLR <IGNORE> on x64

C.M(Boolean)
    L0000: mov eax, 1
    L0005: mov edx, 2
    L000a: test cl, cl
    L000c: cmove eax, edx
    L000f: ret

*/