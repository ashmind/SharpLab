// https://github.com/ashmind/SharpLab/issues/39#issuecomment-298152571
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

static class C {
    static int M(int x) {
        return Foo(x + 0x12345).Result;
    }

    static async Task<int> Foo(int x) {
        return x;
    }
}

/* asm

; Core CLR <IGNORE> on x64

C.M(Int32)
    L0000: sub rsp, 0x28
    L0004: add ecx, 0x12345
    L000a: call 0x<IGNORE>
    L000f: mov rcx, rax
    L0012: mov eax, [rcx+0x34]
    L0015: and eax, 0x<IGNORE>
    L001a: cmp eax, 0x<IGNORE>
    L001f: jne short L0029
    L0021: mov eax, [rcx+0x38]
    L0024: add rsp, 0x28
    L0028: ret
    L0029: mov edx, 1
    L002e: add rsp, 0x28
    L0032: jmp qword ptr [0x<IGNORE>]

C.Foo(Int32)
    L0000: sub rsp, 0x38
    L0004: xor eax, eax
    L0006: mov [rsp+0x28], rax
    L000b: mov [rsp+0x30], rax
    L0010: mov [rsp+0x2c], ecx
    L0014: mov dword ptr [rsp+0x28], 0x<IGNORE>
    L001c: lea rcx, [rsp+0x28]
    L0021: call 0x<IGNORE>
    L0026: mov rax, [rsp+0x30]
    L002b: test rax, rax
    L002e: je short L0035
    L0030: add rsp, 0x38
    L0034: ret
    L0035: lea rcx, [rsp+0x30]
    L003a: call qword ptr [0x<IGNORE>]
    L0040: jmp short L0030

C+<Foo>d__1.MoveNext()
    ; Failed to find JIT output. This might appear more frequently than before due to a library update.
    ; Please monitor https://github.com/ashmind/SharpLab/issues/1334 for progress.

C+<Foo>d__1.SetStateMachine(...)
    ; Failed to find JIT output. This might appear more frequently than before due to a library update.
    ; Please monitor https://github.com/ashmind/SharpLab/issues/1334 for progress.

*/