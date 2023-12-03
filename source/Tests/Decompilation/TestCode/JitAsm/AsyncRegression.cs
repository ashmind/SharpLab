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
    L000a: call C.Foo(Int32)
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
    L0021: call System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[[C+<Foo>d__1, _]](<Foo>d__1 ByRef)
    L0026: mov rax, [rsp+0x30]
    L002b: test rax, rax
    L002e: je short L0035
    L0030: add rsp, 0x38
    L0034: ret
    L0035: lea rcx, [rsp+0x30]
    L003a: call qword ptr [0x<IGNORE>]
    L0040: jmp short L0030

C+<Foo>d__1.MoveNext()
    L0000: push rbp
    L0001: push rsi
    L0002: push rbx
    L0003: sub rsp, 0x30
    L0007: lea rbp, [rsp+0x40]
    L000c: mov [rbp-0x20], rsp
    L0010: mov [rbp+0x10], rcx
    L0014: mov ebx, [rcx+4]
    L0017: mov dword ptr [rcx], 0x<IGNORE>
    L001d: lea rsi, [rcx+8]
    L0021: cmp qword ptr [rsi], 0
    L0025: jne short L007d
    L0027: mov ecx, ebx
    L0029: lea eax, [rbx+1]
    L002c: cmp eax, 0xa
    L002f: jb short L0057
    L0031: mov rcx, 0x<IGNORE>
    L003b: call 0x<IGNORE>
    L0040: mov rdx, rax
    L0043: mov dword ptr [rdx+0x34], 0x<IGNORE>
    L004a: mov [rdx+0x38], ebx
    L004d: mov rcx, rsi
    L0050: call 0x<IGNORE>
    L0055: jmp short L0075
    L0057: mov rax, 0x<IGNORE>
    L0061: mov rax, [rax]
    L0064: lea edx, [rcx+1]
    L0067: cmp edx, 0xa
    L006a: jae short L008a
    L006c: inc ecx
    L006e: mov rdx, [rax+rcx*8+0x10]
    L0073: jmp short L004d
    L0075: add rsp, 0x30
    L0079: pop rbx
    L007a: pop rsi
    L007b: pop rbp
    L007c: ret
    L007d: mov rcx, [rsi]
    L0080: mov edx, ebx
    L0082: call qword ptr [0x<IGNORE>]
    L0088: jmp short L0075
    L008a: call 0x<IGNORE>
    L008f: int3
    L0090: push rbp
    L0091: push rsi
    L0092: push rbx
    L0093: sub rsp, 0x30
    L0097: mov rbp, [rcx+0x20]
    L009b: mov [rsp+0x20], rbp
    L00a0: lea rbp, [rbp+0x40]
    L00a4: mov rcx, [rbp+0x10]
    L00a8: mov dword ptr [rcx], 0x<IGNORE>
    L00ae: add rcx, 8
    L00b2: call qword ptr [0x<IGNORE>]
    L00b8: lea rax, [L0075]
    L00bf: add rsp, 0x30
    L00c3: pop rbx
    L00c4: pop rsi
    L00c5: pop rbp
    L00c6: ret

C+<Foo>d__1.SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine)
    L0000: sub rsp, 0x28
    L0004: mov rcx, [rcx+8]
    L0008: test rdx, rdx
    L000b: je short L0017
    L000d: test rcx, rcx
    L0010: jne short L0023
    L0012: add rsp, 0x28
    L0016: ret
    L0017: mov ecx, 0x3d
    L001c: call qword ptr [0x<IGNORE>]
    L0022: int3
    L0023: mov ecx, 0x28
    L0028: call qword ptr [0x<IGNORE>]
    L002e: int3

*/