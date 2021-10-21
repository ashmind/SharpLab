using System;
using System.Diagnostics;
using System.IO.Pipes;

namespace Fragile {
    public interface IProcessContainer : IDisposable {
        Process Process { get; }
        PipeStream InputStream { get; }
        PipeStream OutputStream { get; }
        PipeStream ErrorStream { get; }
    }
}