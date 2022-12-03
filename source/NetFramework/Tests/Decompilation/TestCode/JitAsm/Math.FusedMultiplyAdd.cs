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
    L0000: vzeroupper
    L0003: vfmadd213sd xmm0, xmm1, xmm2
    L0008: ret

Microsoft.CodeAnalysis.EmbeddedAttribute..ctor()
    L0000: ret

System.Runtime.CompilerServices.RefSafetyRulesAttribute..ctor(Int32)
    L0000: mov [rcx+8], edx
    L0003: ret

*/