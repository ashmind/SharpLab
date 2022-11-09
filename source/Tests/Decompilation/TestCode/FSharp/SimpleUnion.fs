open System

[<Struct;CustomEquality;NoComparison>]
type T =
    | X
    override x.Equals other = false

(* asm

; Core CLR <IGNORE> on x64

_+T..ctor()
    L0000: ret

_+T.get_X()
    L0000: xor eax, eax
    L0002: ret

_+T.get_Tag()
    L0000: xor eax, eax
    L0002: ret

_+T.__DebugDisplay()
    L0000: push r14
    L0002: push rdi
    L0003: push rsi
    L0004: push rbp
    L0005: push rbx
    L0006: sub rsp, 0x20
    L000a: mov rsi, rcx
    L000d: mov rcx, 0x<IGNORE>
    L0017: call 0x<IGNORE>
    L001c: mov rdi, rax
    L001f: mov rdx, 0x<IGNORE>
    L0029: mov rdx, [rdx]
    L002c: lea rcx, [rdi+8]
    L0030: call 0x<IGNORE>
    L0035: xor edx, edx
    L0037: mov [rdi+0x10], rdx
    L003b: mov [rdi+0x18], rdx
    L003f: mov rdx, rdi
    L0042: mov rcx, 0x<IGNORE>
    L004c: call qword ptr [0x<IGNORE>]
    L0052: mov rbx, rax
    L0055: mov rbp, [rdi+0x10]
    L0059: test rbp, rbp
    L005c: jne short L006b
    L005e: mov rcx, rbx
    L0061: cmp [rcx], ecx
    L0063: call qword ptr [0x<IGNORE>]
    L0069: jmp short L00aa
    L006b: mov rcx, rbx
    L006e: cmp [rcx], ecx
    L0070: call qword ptr [0x<IGNORE>]
    L0076: mov r14, rax
    L0079: mov ecx, [rbx+0x28]
    L007c: call qword ptr [0x<IGNORE>]
    L0082: mov rcx, rax
    L0085: mov r8, [rdi+0x18]
    L0089: mov rdx, rbp
    L008c: mov r9, r14
    L008f: cmp [rcx], ecx
    L0091: call qword ptr [0x<IGNORE>]
    L0097: mov rdx, rax
    L009a: mov rcx, 0x<IGNORE>
    L00a4: call qword ptr [0x<IGNORE>]
    L00aa: movsx rdx, byte ptr [rsi]
    L00ae: mov rcx, rax
    L00b1: mov rax, [rax]
    L00b4: mov rax, [rax+0x40]
    L00b8: add rsp, 0x20
    L00bc: pop rbx
    L00bd: pop rbp
    L00be: pop rsi
    L00bf: pop rdi
    L00c0: pop r14
    L00c2: jmp qword ptr [rax+0x20]

_+T.ToString()
    L0000: push r14
    L0002: push rdi
    L0003: push rsi
    L0004: push rbp
    L0005: push rbx
    L0006: sub rsp, 0x20
    L000a: mov rsi, rcx
    L000d: mov rcx, 0x<IGNORE>
    L0017: call 0x<IGNORE>
    L001c: mov rdi, rax
    L001f: mov rdx, 0x<IGNORE>
    L0029: mov rdx, [rdx]
    L002c: lea rcx, [rdi+8]
    L0030: call 0x<IGNORE>
    L0035: xor edx, edx
    L0037: mov [rdi+0x10], rdx
    L003b: mov [rdi+0x18], rdx
    L003f: mov rdx, rdi
    L0042: mov rcx, 0x<IGNORE>
    L004c: call qword ptr [0x<IGNORE>]
    L0052: mov rbx, rax
    L0055: mov rbp, [rdi+0x10]
    L0059: test rbp, rbp
    L005c: jne short L006b
    L005e: mov rcx, rbx
    L0061: cmp [rcx], ecx
    L0063: call qword ptr [0x<IGNORE>]
    L0069: jmp short L00aa
    L006b: mov rcx, rbx
    L006e: cmp [rcx], ecx
    L0070: call qword ptr [0x<IGNORE>]
    L0076: mov r14, rax
    L0079: mov ecx, [rbx+0x28]
    L007c: call qword ptr [0x<IGNORE>]
    L0082: mov rcx, rax
    L0085: mov r8, [rdi+0x18]
    L0089: mov rdx, rbp
    L008c: mov r9, r14
    L008f: cmp [rcx], ecx
    L0091: call qword ptr [0x<IGNORE>]
    L0097: mov rdx, rax
    L009a: mov rcx, 0x<IGNORE>
    L00a4: call qword ptr [0x<IGNORE>]
    L00aa: movsx rdx, byte ptr [rsi]
    L00ae: mov rcx, rax
    L00b1: mov rax, [rax]
    L00b4: mov rax, [rax+0x40]
    L00b8: add rsp, 0x20
    L00bc: pop rbx
    L00bd: pop rbp
    L00be: pop rsi
    L00bf: pop rdi
    L00c0: pop r14
    L00c2: jmp qword ptr [rax+0x20]

_+T.Equals(System.Object)
    L0000: xor eax, eax
    L0002: ret

*)