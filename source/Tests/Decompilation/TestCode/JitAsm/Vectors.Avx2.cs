// https://github.com/ashmind/SharpLab/issues/487
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

public class C
{
    public int M(Vector256<int> vector) {
        var add1 = Sse2.Add(vector.GetLower(), vector.GetUpper());
        return add1.ToScalar();
    }
}

/* asm

; Core CLR <IGNORE> on x64

C..ctor()
    L0000: ret

C.M(System.Runtime.Intrinsics.Vector256`1<Int32>)
    L0000: vmovups ymm0, [rdx]
    L0004: vmovaps ymm1, ymm0
    L0008: vextracti128 xmm0, ymm0, 1
    L000e: vpaddd xmm0, xmm0, xmm1
    L0012: vmovd eax, xmm0
    L0016: vzeroupper
    L0019: ret

*/