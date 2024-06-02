delegate void D();

/* asm

; Core CLR <IGNORE> on x64

D..ctor(...)
    ; Cannot produce JIT assembly for runtime-implemented method.

D.Invoke()
    ; Cannot produce JIT assembly for runtime-implemented method.

D.BeginInvoke(...)
    ; Cannot produce JIT assembly for runtime-implemented method.

D.EndInvoke(...)
    ; Cannot produce JIT assembly for runtime-implemented method.

*/