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

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("0.0.0.0")]
[module: UnverifiableCode]

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

*/