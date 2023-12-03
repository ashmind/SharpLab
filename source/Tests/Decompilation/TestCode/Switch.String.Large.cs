// https://github.com/ashmind/SharpLab/issues/489
public class C
{
    public string M(string key)
    {
        switch (key)
        {
            case "Key1": return "1";
            case "Key2": return "2";
            case "Key3": return "3";
            case "Key4": return "4";
            case "Key5": return "5";
            case "Key6": return "6";
            case "Key7": return "7";
            default: return "?";
        }
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

public class C
{
    [NullableContext(1)]
    public string M(string key)
    {
        if (key != null)
        {
            int length = key.Length;
            if (length == 4)
            {
                switch (key[3])
                {
                    case '1':
                        if (!(key == "Key1"))
                        {
                            break;
                        }
                        return "1";
                    case '2':
                        if (!(key == "Key2"))
                        {
                            break;
                        }
                        return "2";
                    case '3':
                        if (!(key == "Key3"))
                        {
                            break;
                        }
                        return "3";
                    case '4':
                        if (!(key == "Key4"))
                        {
                            break;
                        }
                        return "4";
                    case '5':
                        if (!(key == "Key5"))
                        {
                            break;
                        }
                        return "5";
                    case '6':
                        if (!(key == "Key6"))
                        {
                            break;
                        }
                        return "6";
                    case '7':
                        if (!(key == "Key7"))
                        {
                            break;
                        }
                        return "7";
                }
            }
        }
        return "?";
    }
}

*/