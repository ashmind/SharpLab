public class MyBase {
    public MyBase(string name) {
    }
}


public class MyClass : MyBase {
    public MyClass(string name) : base(name) {
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
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("0.0.0.0")]
[module: UnverifiableCode]
[module: RefSafetyRules(11)]

public class MyBase
{
    [NullableContext(1)]
    public MyBase(string name)
    {
    }
}

public class MyClass : MyBase
{
    [NullableContext(1)]
    public MyClass(string name)
        : base(name)
    {
    }
}

*/