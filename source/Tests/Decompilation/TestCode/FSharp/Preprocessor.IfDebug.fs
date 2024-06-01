#if DEBUG
    printfn "Debug"
#else
    printfn "Release"
#endif

(* cs

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.FSharp.Core;

[assembly: FSharpInterfaceDataVersion(2, 0, 0)]
[assembly: AssemblyVersion("0.0.0.0")]

[CompilationMapping(SourceConstructFlags.Module)]
public static class @_
{
}

namespace <StartupCode$_>
{
    internal static class $_
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [CompilerGenerated]
        [DebuggerNonUserCode]
        internal static int init@;

        public static void main@()
        {
            ExtraTopLevelOperators.PrintFormatLine(new PrintfFormat<Unit, TextWriter, Unit, Unit, Unit>("Debug"));
        }
    }
}

*)