using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using ResourceType = System.Security.AccessControl.ResourceType;

// Based on https://stackoverflow.com/questions/677874/starting-a-process-with-credentials-from-a-windows-service/30687230#30687230
namespace Fragile.Internal.WindowObjectAccessControl {
    [SupportedOSPlatform("windows")]
    internal class WindowObjectSecurity : NativeObjectSecurity {
        private readonly SafeHandle _objectHandle;
        private readonly AccessControlSections _sectionsRequested;

        public WindowObjectSecurity(SafeHandle objectHandle, AccessControlSections sectionsRequested)
            : base(isContainer: false, ResourceType.WindowObject, objectHandle, sectionsRequested) {
            _objectHandle = objectHandle;
            _sectionsRequested = sectionsRequested;
        }

        public void Persist() => Persist(_objectHandle, _sectionsRequested);

        public override Type AccessRightType => throw new NotSupportedException();
        public override Type AccessRuleType => typeof(AccessRule);
        public override Type AuditRuleType => typeof(AuditRule);

        new public void AddAccessRule(AccessRule rule) {
            base.AddAccessRule(rule);
        }

        public override AccessRule AccessRuleFactory(
            IdentityReference identityReference,
            int accessMask, bool isInherited, InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags, AccessControlType type
        ) {
            throw new NotSupportedException();
        }

        public override AuditRule AuditRuleFactory(
            IdentityReference identityReference,
            int accessMask, bool isInherited, InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags, AuditFlags flags
        ) {
            throw new NotSupportedException();
        }
    }
}
