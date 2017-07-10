using System;
using System.Reflection;

namespace SharpLab.Server.Compilation.Internal {
    public static class NetFrameworkRuntime {
        private static readonly Assembly Mscorlib = typeof(object).Assembly;

        public static Assembly AssemblyOfValueTuple { get; }
            = Mscorlib.GetType(typeof(ValueTuple).FullName) != null ? Mscorlib : typeof(ValueTuple).Assembly;
    }
}
