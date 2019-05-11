using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Monitoring {
    public interface IMonitor {
        void Event(string name, IWorkSession? session, IDictionary<string, string>? extras = null);
        void Exception(Exception exception, IWorkSession? session, IDictionary<string, string>? extras = null);
    }
}
