using System;
using ICSharpCode.Decompiler.DebugInfo;

namespace SharpLab.Server.Decompilation.Internal {
    internal interface IDisposableDebugInfoProvider : IDebugInfoProvider, IDisposable {
    }
}
