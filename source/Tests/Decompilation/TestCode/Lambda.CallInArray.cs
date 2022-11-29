using System;
public class C {
	public void M() {
		Func<int, int> getInt = s => s;
		var list = new [] { 1, 2, 3, getInt(1) };
		Console.WriteLine(list[3]);
	}
}

/* cs

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    [Serializable]
    [CompilerGenerated]
    private sealed class <>c
    {
        public static readonly <>c <>9 = new <>c();

        public static Func<int, int> <>9__0_0;

        internal int <M>b__0_0(int s)
        {
            return s;
        }
    }

    public void M()
    {
        Func<int, int> func = <>c.<>9__0_0 ?? (<>c.<>9__0_0 = new Func<int, int>(<>c.<>9.<M>b__0_0));
        int[] array = new int[4];
        RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
        array[3] = func(1);
        Console.WriteLine(array[3]);
    }
}

[CompilerGenerated]
internal sealed class <PrivateImplementationDetails>
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 16)]
    private struct __StaticArrayInitTypeSize=16
    {
    }

    internal static readonly __StaticArrayInitTypeSize=16 81C1A5A2F482E82CA2C66653482AB24E6D90944BF183C8164E8F8F8D72DB60DB/* Not supported: data(01 00 00 00 02 00 00 00 03 00 00 00 00 00 00 00) */;
}

*/