delegate void D();

/* asm

; Desktop CLR <IGNORE> on amd64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+0x8], edx
    L0003: ret

D..ctor(System.Object, IntPtr)
    ; Cannot produce JIT assembly for runtime-implemented method.

D.Invoke()
    ; Cannot produce JIT assembly for runtime-implemented method.

D.BeginInvoke(System.AsyncCallback, System.Object)
    ; Cannot produce JIT assembly for runtime-implemented method.

D.EndInvoke(System.IAsyncResult)
    ; Cannot produce JIT assembly for runtime-implemented method.

*/