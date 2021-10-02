using SharpLab.Runtime;

[JitGeneric(typeof(int))]
[JitGeneric(typeof(decimal))]
[JitGeneric(typeof(string))]
static class C<T> {
    static T M() {
        return default(T);
    }
}

/* asm

; Desktop CLR <IGNORE> on x86

C`1[[System.Int32, mscorlib]].M()
    L0000: xor eax, eax
    L0002: ret

C`1[[System.Decimal, mscorlib]].M()
    L0000: push edi
    L0001: push esi
    L0002: xor eax, eax
    L0004: xor edx, edx
    L0006: xor esi, esi
    L0008: xor edi, edi
    L000a: mov [ecx], eax
    L000c: mov [ecx+0x4], edx
    L000f: mov [ecx+0x8], esi
    L0012: mov [ecx+0xc], edi
    L0015: pop esi
    L0016: pop edi
    L0017: ret

C`1[[System.__Canon, mscorlib]].M()
    L0000: xor eax, eax
    L0002: ret

*/