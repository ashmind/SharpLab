public class C {
    public void M() {
        using (new System.IO.MemoryStream()) {
        }
    }
}

/* cs

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue | DebuggableAttribute.DebuggingModes.DisableOptimizations)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("0.0.0.0")]
[module: UnverifiableCode]
public class C
{
    public void M()
    {
        MemoryStream memoryStream = new MemoryStream();
        try
        {
        }
        finally
        {
            if (memoryStream != null)
            {
                ((IDisposable)memoryStream).Dispose();
            }
        }
    }
}

*/