static class C {
    static int M(int[] x) {
        return x[0];
    }
}

/* asm

; Desktop CLR <IGNORE> on amd64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+0x8], edx
    L0003: ret

C.M(Int32[])
    L0000: sub rsp, 0x28
    L0004: cmp dword [rcx+0x8], 0x0
    L0008: jbe L0012
    L000a: mov eax, [rcx+0x10]
    L000d: add rsp, 0x28
    L0011: ret
    L0012: call 0x<IGNORE>
    L0017: int3

*/