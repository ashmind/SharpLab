using System;
static class C {
    static void M() => Console.WriteLine("test");
}

/* asm

; Core CLR <IGNORE> on x64

C.M()
    L0000: mov rcx, 0x<IGNORE>
    L000a: mov rcx, [rcx]
    L000d: jmp qword ptr [0x<IGNORE>]

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+8], edx
    L0003: ret

*/