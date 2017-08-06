using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Monitoring {
    public interface IMonitor {
        void Event([NotNull] string name, [CanBeNull] IWorkSession session, [CanBeNull] IDictionary<string, string> extras = null);
        void Exception([NotNull] Exception exception, [CanBeNull] IWorkSession session, [CanBeNull] IDictionary<string, string> extras = null);
    }
}
