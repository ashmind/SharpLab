type Empty = class end

(* il

.assembly _
{
    .custom instance void [FSharp.Core]Microsoft.FSharp.Core.FSharpInterfaceDataVersionAttribute::.ctor(int32, int32, int32) = (
        01 00 02 00 00 00 00 00 00 00 00 00 00 00 00 00
    )
    .hash algorithm 0x<IGNORE> // SHA1
    .ver 0:0:0:0
}

.class private auto ansi '<Module>'
    extends [mscorlib]System.Object
{
} // end of class <Module>

.class public auto ansi abstract sealed _
    extends [mscorlib]System.Object
{
    .custom instance void [FSharp.Core]Microsoft.FSharp.Core.CompilationMappingAttribute::.ctor(valuetype [FSharp.Core]Microsoft.FSharp.Core.SourceConstructFlags) = (
        01 00 07 00 00 00 00 00
    )
    // Nested Types
    .class nested public auto ansi serializable Empty
        extends [mscorlib]System.Object
    {
        .custom instance void [FSharp.Core]Microsoft.FSharp.Core.CompilationMappingAttribute::.ctor(valuetype [FSharp.Core]Microsoft.FSharp.Core.SourceConstructFlags) = (
            01 00 03 00 00 00 00 00
        )
    } // end of class Empty


} // end of class _

.class private auto ansi abstract sealed '<StartupCode$_>.$_'
    extends [mscorlib]System.Object
{
    // Methods
    .method public static 
        void main@ () cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 1 (0x1)
        .maxstack 8
        .entrypoint

        IL_0000: ret
    } // end of method $_::main@

} // end of class <StartupCode$_>.$_

*)