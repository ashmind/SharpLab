public class C
{
    public void M()
    {
        string one = $"This {1} That";
        string two = $"This {one} That";
    }
}

/* cs

using System.Diagnostics;
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
        DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 1);
        defaultInterpolatedStringHandler.AppendLiteral("This ");
        defaultInterpolatedStringHandler.AppendFormatted(1);
        defaultInterpolatedStringHandler.AppendLiteral(" That");
        string text = defaultInterpolatedStringHandler.ToStringAndClear();
        string text2 = string.Concat("This ", text, " That");
    }
}

*/