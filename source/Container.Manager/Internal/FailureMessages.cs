using System.Text;
using System;

namespace SharpLab.Container.Manager.Internal {
    public static class FailureMessages {
        public static readonly ReadOnlyMemory<byte> TimedOut = Encoding.UTF8.GetBytes("\n(Execution timed out)");
        public static readonly ReadOnlyMemory<byte> IOFailure = Encoding.UTF8.GetBytes("\n(Unexpected IO failure)");
    }
}
