using System;
public class C {
    public bool M(DateTime? d) {
        return d > DateTime.Now;
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
    public bool M(Nullable<DateTime> d)
    {
        Nullable<DateTime> dateTime = d;
        DateTime now = DateTime.Now;
        if (!dateTime.HasValue)
        {
            return false;
        }
        return dateTime.GetValueOrDefault() > now;
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