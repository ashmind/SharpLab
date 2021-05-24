using System;
using System.Diagnostics;

namespace SharpLab.Container.Runtime {
    internal static class Current {
        public static readonly int ProcessId = ((Func<int>)(() => {
            using var current = Process.GetCurrentProcess();
            return current.Id;
        }))();
    }
}