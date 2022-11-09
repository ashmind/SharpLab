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

; Core CLR <IGNORE> on x64

C..ctor()
    L0000: ret

C.M(Int32)
    L0000: inc edx
    L0002: je short L0000
    L0004: mov eax, edx
    L0006: ret

*/