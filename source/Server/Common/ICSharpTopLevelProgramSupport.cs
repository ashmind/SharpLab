using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Compilation {
    public interface ICSharpTopLevelProgramSupport {
        void UpdateOutputKind(IWorkSession session, IList<Diagnostic>? diagnostics = null);
    }
}