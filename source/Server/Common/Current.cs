using System;
using System.Diagnostics;
using AshMind.Extensions;

namespace SharpLab.Server.Common {
    public static class Current {
        public static readonly int ProcessId = Environment.ProcessId;

        public static readonly string AssemblyPath = typeof(Current).Assembly.GetAssemblyFile().FullName;
    }
}