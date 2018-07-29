using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.DebugInfo;

namespace SharpLab.Server.Decompilation.Internal {
    public interface IDisposableDebugInfoProvider : IDebugInfoProvider, IDisposable {
    }
}
