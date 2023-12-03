public class Foo {
    public static explicit operator uint? (Foo foo) => 1;
}

public class Bar {
    public void Baz() {
        var x = (uint?)((Foo)null);
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
[module: RefSafetyRules(11)]

public class Foo
{
    [NullableContext(1)]
    public static explicit operator Nullable<uint>(Foo foo)
    {
        return 1u;
    }
}

public class Bar
{
    public void Baz()
    {
        Nullable<uint> num = (Nullable<uint>)(Foo)null;
    }
}

*/