using System;
using ICSharpCode.Decompiler.DebugInfo;

namespace SharpLab.Server.Decompilation.Internal {
    public interface IDisposableDebugInfoProvider : IDebugInfoProvider, IDisposable {
    }
}
