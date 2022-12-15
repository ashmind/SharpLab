using MirrorSharp.Advanced;
using System;

namespace SharpLab.Server.Common {
    public interface IExceptionLogFilter {
        bool ShouldLog(Exception exception, IWorkSession session);
    }
}
