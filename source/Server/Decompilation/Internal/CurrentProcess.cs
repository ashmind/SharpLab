using System;
using System.Diagnostics;

namespace SharpLab.Server.Decompilation.Internal {
    public static class CurrentProcess {
        public static readonly int Id = ((Func<int>)(() => {
            using (var current = Process.GetCurrentProcess()) {
                return current.Id;
            }
        }))();
    }
}