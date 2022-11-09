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

C.M(Int32)
    L0000: push rdi
    L0001: push rsi
    L0002: sub rsp, 0x28
    L0006: add ecx, 0x12345
    L000c: call C.Foo(Int32)
    L0011: mov rsi, rax
    L0014: mov ecx, [rsi+0x34]
    L0017: and ecx, 0x<IGNORE>
    L001d: cmp ecx, 0x<IGNORE>
    L0023: jne short L002a
    L0025: mov eax, [rsi+0x38]
    L0028: jmp short L0077
    L002a: test dword ptr [rsi+0x34], 0x<IGNORE>
    L0031: jne short L0044
    L0033: mov rcx, rsi
    L0036: xor r8d, r8d
    L0039: mov edx, 0x<IGNORE>
    L003e: call qword ptr [0x<IGNORE>]
    L0044: mov rcx, rsi
    L0047: call qword ptr [0x<IGNORE>]
    L004d: mov ecx, [rsi+0x34]
    L0050: and ecx, 0x<IGNORE>
    L0056: cmp ecx, 0x<IGNORE>
    L005c: je short L0074
    L005e: mov rcx, rsi
    L0061: mov edx, 1
    L0066: call qword ptr [0x<IGNORE>]
    L006c: mov rdi, rax
    L006f: test rdi, rdi
    L0072: jne short L007e
    L0074: mov eax, [rsi+0x38]
    L0077: add rsp, 0x28
    L007b: pop rsi
    L007c: pop rdi
    L007d: ret
    L007e: mov rcx, rsi
    L0081: call qword ptr [0x<IGNORE>]
    L0087: mov rcx, rdi
    L008a: call 0x<IGNORE>
    L008f: int3

C.Foo(Int32)
    L0000: sub rsp, 0x38
    L0004: xor eax, eax
    L0006: mov [rsp+0x28], rax
    L000b: mov [rsp+0x30], rax
    L0010: xor eax, eax
    L0012: mov [rsp+0x30], rax
    L0017: mov [rsp+0x2c], ecx
    L001b: mov dword ptr [rsp+0x28], 0x<IGNORE>
    L0023: lea rcx, [rsp+0x28]
    L0028: call System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[[C+<Foo>d__1, _]](<Foo>d__1 ByRef)
    L002d: mov rax, [rsp+0x30]
    L0032: test rax, rax
    L0035: je short L003c
    L0037: add rsp, 0x38
    L003b: ret
    L003c: lea rcx, [rsp+0x30]
    L0041: call qword ptr [0x<IGNORE>]
    L0047: jmp short L0037

C+<Foo>d__1.MoveNext()
    L0000: push rbp
    L0001: push rdi
    L0002: push rsi
    L0003: sub rsp, 0x30
    L0007: lea rbp, [rsp+0x40]
    L000c: mov [rbp-0x20], rsp
    L0010: mov [rbp+0x10], rcx
    L0014: mov esi, [rcx+4]
    L0017: mov dword ptr [rcx], 0x<IGNORE>
    L001d: lea rdi, [rcx+8]
    L0021: cmp qword ptr [rdi], 0
    L0025: jne short L007f
    L0027: mov ecx, esi
    L0029: lea eax, [rcx+1]
    L002c: cmp eax, 0xa
    L002f: jb short L0057
    L0031: mov rcx, 0x<IGNORE>
    L003b: call 0x<IGNORE>
    L0040: mov rdx, rax
    L0043: mov dword ptr [rdx+0x34], 0x<IGNORE>
    L004a: mov [rdx+0x38], esi
    L004d: mov rcx, rdi
    L0050: call 0x<IGNORE>
    L0055: jmp short L0077
    L0057: mov rax, 0x<IGNORE>
    L0061: mov rax, [rax]
    L0064: lea edx, [rcx+1]
    L0067: cmp edx, [rax+8]
    L006a: jae short L008c
    L006c: inc ecx
    L006e: mov ecx, ecx
    L0070: mov rdx, [rax+rcx*8+0x10]
    L0075: jmp short L004d
    L0077: add rsp, 0x30
    L007b: pop rsi
    L007c: pop rdi
    L007d: pop rbp
    L007e: ret
    L007f: mov rcx, [rdi]
    L0082: mov edx, esi
    L0084: call qword ptr [0x<IGNORE>]
    L008a: jmp short L0077
    L008c: call 0x<IGNORE>
    L0091: int3
    L0092: push rbp
    L0093: push rdi
    L0094: push rsi
    L0095: sub rsp, 0x30
    L0099: mov rbp, [rcx+0x20]
    L009d: mov [rsp+0x20], rbp
    L00a2: lea rbp, [rbp+0x40]
    L00a6: mov rcx, [rbp+0x10]
    L00aa: mov dword ptr [rcx], 0x<IGNORE>
    L00b0: add rcx, 8
    L00b4: call qword ptr [0x<IGNORE>]
    L00ba: lea rax, [L0077]
    L00c1: add rsp, 0x30
    L00c5: pop rsi
    L00c6: pop rdi
    L00c7: pop rbp
    L00c8: ret

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
    L0023: mov ecx, 0x27
    L0028: call qword ptr [0x<IGNORE>]
    L002e: int3

*/