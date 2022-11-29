using System;
public class C {
    public string M() {
    #if DEBUG
        return "Debug";
    #else
        return "Release";
    #endif
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
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue | DebuggableAttribute.DebuggingModes.DisableOptimizations)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("0.0.0.0")]
[module: UnverifiableCode]
public class C
{
    [System.Runtime.CompilerServices.NullableContext(1)]
    public string M()
    {
        return "Debug";
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
}

*/