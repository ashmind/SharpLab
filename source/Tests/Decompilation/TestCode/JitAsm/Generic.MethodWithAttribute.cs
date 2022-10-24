using SharpLab.Runtime;
static class C {
    [JitGeneric(typeof(int))]
    [JitGeneric(typeof(decimal))]
    [JitGeneric(typeof(string))]
    static T M<T>() {
        return default(T);
    }
}

/* asm

; Core CLR <IGNORE> on amd64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.NullableAttribute..ctor(Byte)
    L0000: push rdi
    L0001: push rsi
    L0002: sub rsp, 0x28
    L0006: mov rsi, rcx
    L0009: mov edi, edx
    L000b: mov rcx, 0x<IGNORE>
    L0015: mov edx, 1
    L001a: call 0x<IGNORE>
    L001f: mov [rax+0x10], dil
    L0023: lea rcx, [rsi+8]
    L0027: mov rdx, rax
    L002a: call 0x<IGNORE>
    L002f: nop
    L0030: add rsp, 0x28
    L0034: pop rsi
    L0035: pop rdi
    L0036: ret

System.Runtime.CompilerServices.NullableAttribute..ctor(Byte[])
    L0000: lea rcx, [rcx+8]
    L0004: call 0x<IGNORE>
    L0009: nop
    L000a: ret

System.Runtime.CompilerServices.NullableContextAttribute..ctor(Byte)
    L0000: mov [rcx+8], dl
    L0003: ret

C.M[[System.Int32, System.Private.CoreLib]]()
    L0000: xor eax, eax
    L0002: ret

C.M[[System.Decimal, System.Private.CoreLib]]()
    L0000: xor eax, eax
    L0002: mov [rcx], eax
    L0004: mov [rcx+4], eax
    L0007: mov [rcx+8], rax
    L000b: mov rax, rcx
    L000e: ret

C.M[[System.String, System.Private.CoreLib]]()
    ; Failed to find JIT output for generic method (reference types?).
    ; If you know a solution, please comment at https://github.com/ashmind/SharpLab/issues/99.

*/