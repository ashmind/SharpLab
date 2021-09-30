using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

// Based on https://stackoverflow.com/questions/677874/starting-a-process-with-credentials-from-a-windows-service/30687230#30687230
namespace Fragile.Internal.WindowObjectAccessControl {
    [SupportedOSPlatform("windows")]
    internal class WindowObjectAccessRule : AccessRule {
        public WindowObjectAccessRule(IdentityReference identity, int accessMask, AccessControlType type)
            : base(identity, accessMask, false, InheritanceFlags.None, PropagationFlags.None, type) {
        }
    }
}
