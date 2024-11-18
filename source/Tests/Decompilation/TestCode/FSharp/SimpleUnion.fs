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
    L000a: mov rbx, rcx
    L000d: mov rcx, 0x<IGNORE>
    L0017: call 0x<IGNORE>
    L001c: mov rsi, rax
    L001f: mov rcx, 0x<IGNORE>
    L0029: mov rdx, [rcx]
    L002c: lea rcx, [rsi+8]
    L0030: call 0x<IGNORE>
    L0035: xor edx, edx
    L0037: mov [rsi+0x10], rdx
    L003b: mov [rsi+0x18], rdx
    L003f: mov rdx, rsi
    L0042: mov rcx, 0x<IGNORE>
    L004c: call qword ptr [0x<IGNORE>]
    L0052: mov rdi, rax
    L0055: mov rbp, [rsi+0x10]
    L0059: test rbp, rbp
    L005c: jne short L0084
    L005e: mov rcx, rdi
    L0061: cmp [rcx], ecx
    L0063: call qword ptr [0x<IGNORE>]
    L0069: movzx edx, byte ptr [rbx]
    L006c: mov rcx, rax
    L006f: mov rax, [rax]
    L0072: mov rax, [rax+0x40]
    L0076: add rsp, 0x20
    L007a: pop rbx
    L007b: pop rbp
    L007c: pop rsi
    L007d: pop rdi
    L007e: pop r14
    L0080: jmp qword ptr [rax+0x20]
    L0084: mov rcx, rdi
    L0087: cmp [rcx], ecx
    L0089: call qword ptr [0x<IGNORE>]
    L008f: mov r14, rax
    L0092: mov ecx, [rdi+0x28]
    L0095: call qword ptr [0x<IGNORE>]
    L009b: mov rcx, rax
    L009e: mov r8, [rsi+0x18]
    L00a2: mov rdx, rbp
    L00a5: mov r9, r14
    L00a8: cmp [rcx], ecx
    L00aa: call qword ptr [0x<IGNORE>]
    L00b0: mov rdx, rax
    L00b3: mov rcx, 0x<IGNORE>
    L00bd: call qword ptr [0x<IGNORE>]
    L00c3: jmp short L0069

_+T.ToString()
    ; Failed to find JIT output. This might appear more frequently than before due to a library update.
    ; Please monitor https://github.com/ashmind/SharpLab/issues/1334 for progress.

_+T.Equals(...)
    ; Failed to find JIT output. This might appear more frequently than before due to a library update.
    ; Please monitor https://github.com/ashmind/SharpLab/issues/1334 for progress.

<StartupCode$_>.$_.main@()
    L0000: ret

*)