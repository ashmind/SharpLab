open System
type C() =
    member this.M() = 5

(* cs

using System;
using System.Reflection;
using Microsoft.FSharp.Core;

[assembly: FSharpInterfaceDataVersion(2, 0, 0)]
[assembly: AssemblyVersion("0.0.0.0")]

[CompilationMapping(SourceConstructFlags.Module)]
public static class @_
{
    [Serializable]
    [CompilationMapping(SourceConstructFlags.ObjectType)]
    public class C
    {
        public int M()
        {
            return 5;
        }
    }
}

namespace <StartupCode$_>
{
    internal static class $_
    {
    }
}

*)