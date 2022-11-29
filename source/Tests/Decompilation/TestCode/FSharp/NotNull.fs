open System

type C() =
    member __.notNull x = not (isNull x)

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
        public bool notNull<a>(a x) where a : class
        {
            if (x == null)
            {
                return false;
            }
            return true;
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