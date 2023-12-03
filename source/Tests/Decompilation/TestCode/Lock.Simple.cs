// https://github.com/ashmind/SharpLab/issues/521
using System;

public class C {
    public void M(object o) {
        lock (o) {
            Console.WriteLine();
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
using System.Threading;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("0.0.0.0")]
[module: UnverifiableCode]
[module: RefSafetyRules(11)]

public class C
{
    [NullableContext(1)]
    public void M(object o)
    {
        bool lockTaken = false;
        try
        {
            Monitor.Enter(o, ref lockTaken);
            Console.WriteLine();
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(o);
            }
        }
    }
}

*/