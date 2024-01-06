using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Compilation; 
public interface ICompiler {
    Task<(bool assembly, bool symbols)> TryCompileToStreamAsync(
        MemoryStream assemblyStream,
        MemoryStream? symbolStream,
        IWorkSession session,
        IList<Diagnostic> diagnostics,
        CancellationToken cancellationToken
    );
}