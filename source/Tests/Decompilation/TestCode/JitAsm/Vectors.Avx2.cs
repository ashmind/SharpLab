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

; Core CLR <IGNORE> on amd64

C..ctor()
    L0000: ret

C.M(System.Runtime.Intrinsics.Vector256`1<Int32>)
    L0000: vzeroupper
    L0003: vmovupd ymm0, [rdx]
    L0007: vextracti128 xmm0, ymm0, 1
    L000d: vmovdqu ymm1, [rdx]
    L0011: vpaddd xmm0, xmm1, xmm0
    L0015: vmovd eax, xmm0
    L0019: vzeroupper
    L001c: ret

*/