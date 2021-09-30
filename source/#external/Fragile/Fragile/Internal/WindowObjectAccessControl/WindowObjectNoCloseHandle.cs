using System.Runtime.InteropServices;
using Vanara.PInvoke;

// Based on https://stackoverflow.com/questions/677874/starting-a-process-with-credentials-from-a-windows-service/30687230#30687230
namespace Fragile.Internal.WindowObjectAccessControl {
    // Handles returned by GetProcessWindowStation and GetThreadDesktop
    // should not be closed
    internal class WindowObjectNoCloseHandle : SafeHandle {
        public WindowObjectNoCloseHandle(HWINSTA windowStationHandle)
            : base(windowStationHandle.DangerousGetHandle(), false)
        {
        }

        public WindowObjectNoCloseHandle(HDESK desktopHandle)
            : base(desktopHandle.DangerousGetHandle(), false)
        {
        }

        public override bool IsInvalid => false;
        protected override bool ReleaseHandle() => true;
    }
}
