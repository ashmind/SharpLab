delegate void D();

/* asm

; Core CLR <IGNORE> on x64

D..ctor(System.Object, IntPtr)
    ; Cannot produce JIT assembly for runtime-implemented method.

D.Invoke()
    ; Cannot produce JIT assembly for runtime-implemented method.

D.BeginInvoke(System.AsyncCallback, System.Object)
    ; Cannot produce JIT assembly for runtime-implemented method.

D.EndInvoke(System.IAsyncResult)
    ; Cannot produce JIT assembly for runtime-implemented method.

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+8], edx
    L0003: ret

*/