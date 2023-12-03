public class Point {
    public int X { get; set; }
    public int M(Point p) {
        return (p?.X).GetValueOrDefault();
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
[module: RefSafetyRules(11)]
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
    [Embedded]
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
public class Point
{
    [CompilerGenerated]
    private int <X>k__BackingField;

    public int X
    {
        [CompilerGenerated]
        get
        {
            return <X>k__BackingField;
        }
        [CompilerGenerated]
        set
        {
            <X>k__BackingField = value;
        }
    }

    public int M(Point p)
    {
        Nullable<int> num = ((p != null) ? new Nullable<int>(p.X) : null);
        return num.GetValueOrDefault();
    }
}

*/