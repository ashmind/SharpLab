using System;
using System.Diagnostics;
using System.IO;

namespace Fragile {
    public interface IProcessContainer : IDisposable {
        Process Process { get; }
        Stream InputStream { get; }
        Stream OutputStream { get; }
        Stream ErrorStream { get; }


    }
}