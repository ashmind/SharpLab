// https://github.com/ashmind/SharpLab/issues/489
public class C
{
    public string M(string key)
    {
        switch (key)
        {
            case "Key1": return "1";
            case "Key2": return "2";
            case "Key3": return "3";
            case "Key4": return "4";
            case "Key5": return "5";
            case "Key6": return "6";
            case "Key7": return "7";
            default: return "?";
        }
    }
}

/* cs

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using Microsoft.CodeAnalysis;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("0.0.0.0")]
[module: UnverifiableCode]
[module: System.Runtime.CompilerServices.RefSafetyRules(11)]

public class C
{
    [System.Runtime.CompilerServices.NullableContext(1)]
    public string M(string key)
    {
        uint num = <PrivateImplementationDetails>.ComputeStringHash(key);
        if (num <= 455788110)
        {
            if (num != 422232872)
            {
                if (num != 439010491)
                {
                    if (num == 455788110 && key == "Key6")
                    {
                        return "6";
                    }
                }
                else if (key == "Key5")
                {
                    return "5";
                }
            }
            else if (key == "Key4")
            {
                return "4";
            }
        }
        else if (num <= 506120967)
        {
            if (num != 472565729)
            {
                if (num == 506120967 && key == "Key1")
                {
                    return "1";
                }
            }
            else if (key == "Key7")
            {
                return "7";
            }
        }
        else if (num != 522898586)
        {
            if (num == 539676205 && key == "Key3")
            {
                return "3";
            }
        }
        else if (key == "Key2")
        {
            return "2";
        }
        return "?";
    }
}

[CompilerGenerated]
internal sealed class <PrivateImplementationDetails>
{
    internal static uint ComputeStringHash(string s)
    {
        uint num = default(uint);
        if (s != null)
        {
            num = 2166136261u;
            int num2 = 0;
            while (num2 < s.Length)
            {
                num = (s[num2] ^ num) * 16777619;
                num2++;
            }
        }
        return num;
    }
}

namespace Microsoft.CodeAnalysis
{
    [CompilerGenerated]
    [Embedded]
    internal sealed class EmbeddedAttribute : Attribute
    {
    }
}

namespace System.Runtime.CompilerServices
{
    [CompilerGenerated]
    [Microsoft.CodeAnalysis.Embedded]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
    internal sealed class NullableAttribute : Attribute
    {
        public readonly byte[] NullableFlags;

        public NullableAttribute(byte P_0)
        {
            byte[] array = new byte[1];
            array[0] = P_0;
            NullableFlags = array;
        }

        public NullableAttribute(byte[] P_0)
        {
            NullableFlags = P_0;
        }
    }

    [CompilerGenerated]
    [Microsoft.CodeAnalysis.Embedded]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
    internal sealed class NullableContextAttribute : Attribute
    {
        public readonly byte Flag;

        public NullableContextAttribute(byte P_0)
        {
            Flag = P_0;
        }
    }

    [CompilerGenerated]
    [Microsoft.CodeAnalysis.Embedded]
    [AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
    internal sealed class RefSafetyRulesAttribute : Attribute
    {
        public readonly int Version;

        public RefSafetyRulesAttribute(int P_0)
        {
            Version = P_0;
        }
    }
}

*/