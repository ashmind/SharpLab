using System;
public class C {
    ~C() {
        throw new Exception();
    }
}

/* il

.assembly _
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = (
        01 00 08 00 00 00 00 00
    )
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = (
        01 00 01 00 54 02 16 57 72 61 70 4e 6f 6e 45 78
        63 65 70 74 69 6f 6e 54 68 72 6f 77 73 01
    )
    .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = (
        01 00 02 00 00 00 00 00
    )
    .permissionset reqmin = (
        2e 01 80 8a 53 79 73 74 65 6d 2e 53 65 63 75 72
        69 74 79 2e 50 65 72 6d 69 73 73 69 6f 6e 73 2e
        53 65 63 75 72 69 74 79 50 65 72 6d 69 73 73 69
        6f 6e 41 74 74 72 69 62 75 74 65 2c 20 53 79 73
        74 65 6d 2e 52 75 6e 74 69 6d 65 2c 20 56 65 72
        73 69 6f 6e 3d 37 2e 30 2e 30 2e 30 2c 20 43 75
        6c 74 75 72 65 3d 6e 65 75 74 72 61 6c 2c 20 50
        75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e 3d 62 30
        33 66 35 66 37 66 31 31 64 35 30 61 33 61 15 01
        54 02 10 53 6b 69 70 56 65 72 69 66 69 63 61 74
        69 6f 6e 01
    )
    .hash algorithm 0x<IGNORE> // SHA1
    .ver 0:0:0:0
}

.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi beforefieldinit C
    extends [System.Runtime]System.Object
{
    // Methods
    .method family hidebysig virtual 
        instance void Finalize () cil managed 
    {
        .override method instance void [System.Runtime]System.Object::Finalize()
        // Method begins at RVA 0x2068
        // Code size 13 (0xd)
        .maxstack 1

        .try
        {
            // sequence point: (line 4, col 9) to (line 4, col 31) in _
            IL_0000: newobj instance void [System.Runtime]System.Exception::.ctor()
            IL_0005: throw
        } // end .try
        finally
        {
            // sequence point: (line 5, col 5) to (line 5, col 6) in _
            IL_0006: ldarg.0
            IL_0007: call instance void [System.Runtime]System.Object::Finalize()
            IL_000c: endfinally
        } // end handler
    } // end of method C::Finalize

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2094
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: ret
    } // end of method C::.ctor

} // end of class C

.class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
    extends [System.Runtime]System.Attribute
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
        01 00 00 00
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Attribute::.ctor()
        IL_0006: ret
    } // end of method EmbeddedAttribute::.ctor

} // end of class Microsoft.CodeAnalysis.EmbeddedAttribute

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RefSafetyRulesAttribute
    extends [System.Runtime]System.Attribute
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [System.Runtime]System.AttributeUsageAttribute::.ctor(valuetype [System.Runtime]System.AttributeTargets) = (
        01 00 02 00 00 00 02 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00 54 02 09 49 6e 68 65
        72 69 74 65 64 00
    )
    // Fields
    .field public initonly int32 Version

    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 ''
        ) cil managed 
    {
        // Method begins at RVA 0x2058
        // Code size 14 (0xe)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Attribute::.ctor()
        IL_0006: ldarg.0
        IL_0007: ldarg.1
        IL_0008: stfld int32 System.Runtime.CompilerServices.RefSafetyRulesAttribute::Version
        IL_000d: ret
    } // end of method RefSafetyRulesAttribute::.ctor

} // end of class System.Runtime.CompilerServices.RefSafetyRulesAttribute

*/