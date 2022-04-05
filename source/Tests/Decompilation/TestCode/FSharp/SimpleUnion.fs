open System

[<Struct;CustomEquality;NoComparison>]
type T =
    | X
    override x.Equals other = false

(* asm
; Core CLR <IGNORE> on amd64

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
    L004c: call Microsoft.FSharp.Core.PrintfImpl+Cache`4[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].GetParser(Microsoft.FSharp.Core.PrintfFormat`4<System.__Canon,System.__Canon,System.__Canon,System.__Canon>)
    L0051: mov rbx, rax
    L0054: mov rbp, [rdi+0x10]
    L0058: test rbp, rbp
    L005b: jne short L0069
    L005d: mov rcx, rbx
    L0060: cmp [rcx], ecx
    L0062: call Microsoft.FSharp.Core.PrintfImpl+FormatParser`4[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].GetCurriedStringPrinter()
    L0067: jmp short L00a4
    L0069: mov rcx, rbx
    L006c: cmp [rcx], ecx
    L006e: call Microsoft.FSharp.Core.PrintfImpl+FormatParser`4[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].GetStepsForCapturedFormat()
    L0073: mov r14, rax
    L0076: mov ecx, [rbx+0x28]
    L0079: call Microsoft.FSharp.Core.PrintfImpl.StringPrintfEnv(Int32)
    L007e: mov rcx, rax
    L0081: mov r8, [rdi+0x18]
    L0085: mov rdx, rbp
    L0088: mov r9, r14
    L008b: cmp [rcx], ecx
    L008d: call Microsoft.FSharp.Core.PrintfImpl+PrintfEnv`3[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].RunSteps(System.Object[], System.Type[], Step[])
    L0092: mov rdx, rax
    L0095: mov rcx, 0x<IGNORE>
    L009f: call Microsoft.FSharp.Core.LanguagePrimitives+IntrinsicFunctions.UnboxGeneric[[System.__Canon, System.Private.CoreLib]](System.Object)
    L00a4: movsx rdx, byte ptr [rsi]
    L00a8: mov rcx, rax
    L00ab: mov rax, [rax]
    L00ae: mov rax, [rax+0x40]
    L00b2: mov rax, [rax+0x20]
    L00b6: add rsp, 0x20
    L00ba: pop rbx
    L00bb: pop rbp
    L00bc: pop rsi
    L00bd: pop rdi
    L00be: pop r14
    L00c0: jmp rax

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
    L004c: call Microsoft.FSharp.Core.PrintfImpl+Cache`4[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].GetParser(Microsoft.FSharp.Core.PrintfFormat`4<System.__Canon,System.__Canon,System.__Canon,System.__Canon>)
    L0051: mov rbx, rax
    L0054: mov rbp, [rdi+0x10]
    L0058: test rbp, rbp
    L005b: jne short L0069
    L005d: mov rcx, rbx
    L0060: cmp [rcx], ecx
    L0062: call Microsoft.FSharp.Core.PrintfImpl+FormatParser`4[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].GetCurriedStringPrinter()
    L0067: jmp short L00a4
    L0069: mov rcx, rbx
    L006c: cmp [rcx], ecx
    L006e: call Microsoft.FSharp.Core.PrintfImpl+FormatParser`4[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].GetStepsForCapturedFormat()
    L0073: mov r14, rax
    L0076: mov ecx, [rbx+0x28]
    L0079: call Microsoft.FSharp.Core.PrintfImpl.StringPrintfEnv(Int32)
    L007e: mov rcx, rax
    L0081: mov r8, [rdi+0x18]
    L0085: mov rdx, rbp
    L0088: mov r9, r14
    L008b: cmp [rcx], ecx
    L008d: call Microsoft.FSharp.Core.PrintfImpl+PrintfEnv`3[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].RunSteps(System.Object[], System.Type[], Step[])
    L0092: mov rdx, rax
    L0095: mov rcx, 0x<IGNORE>
    L009f: call Microsoft.FSharp.Core.LanguagePrimitives+IntrinsicFunctions.UnboxGeneric[[System.__Canon, System.Private.CoreLib]](System.Object)
    L00a4: movsx rdx, byte ptr [rsi]
    L00a8: mov rcx, rax
    L00ab: mov rax, [rax]
    L00ae: mov rax, [rax+0x40]
    L00b2: mov rax, [rax+0x20]
    L00b6: add rsp, 0x20
    L00ba: pop rbx
    L00bb: pop rbp
    L00bc: pop rsi
    L00bd: pop rdi
    L00be: pop r14
    L00c0: jmp rax

_+T.Equals(System.Object)
    L0000: xor eax, eax
    L0002: ret
*)