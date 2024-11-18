// https://github.com/ashmind/SharpLab/issues/458
using System;
public static class C {
    public static double M(double a, double b, double c) {
        return Math.FusedMultiplyAdd(a, b, c);
    }
}

/* asm

; Core CLR <IGNORE> on x64

C.M(Double, Double, Double)
    L0000: vfmadd213sd xmm0, xmm1, xmm2
    L0005: ret

*/