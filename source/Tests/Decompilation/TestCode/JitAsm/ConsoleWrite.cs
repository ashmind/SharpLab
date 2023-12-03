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

*/