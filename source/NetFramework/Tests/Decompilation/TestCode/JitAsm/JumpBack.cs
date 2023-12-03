// https://github.com/ashmind/SharpLab/issues/229
public class C
{
    public int M(int a) {
        back:
        a += 1;
        if (a == 0)
            goto back;
        return a;
    }
}

/* asm

; Desktop CLR <IGNORE> on amd64

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+0x8], edx
    L0003: ret

C..ctor()
    L0000: ret

C.M(Int32)
    L0000: inc edx
    L0002: test edx, edx
    L0004: jz L0000
    L0006: mov eax, edx
    L0008: ret

*/