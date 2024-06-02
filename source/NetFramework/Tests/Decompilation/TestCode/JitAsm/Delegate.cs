delegate void D();

/* asm

; Desktop CLR <IGNORE> on x64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+0x8], edx
    L0003: ret

Unknown (0x<IGNORE>)
    ; Method signature was not found -- please report this issue.
    ; Cannot produce JIT assembly for runtime-implemented method.

Unknown (0x<IGNORE>)
    ; Method signature was not found -- please report this issue.
    ; Cannot produce JIT assembly for runtime-implemented method.

Unknown (0x<IGNORE>)
    ; Method signature was not found -- please report this issue.
    ; Cannot produce JIT assembly for runtime-implemented method.

Unknown (0x<IGNORE>)
    ; Method signature was not found -- please report this issue.
    ; Cannot produce JIT assembly for runtime-implemented method.

*/