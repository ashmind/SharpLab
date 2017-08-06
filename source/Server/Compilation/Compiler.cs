using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Control;
using MirrorSharp.Advanced;
using MirrorSharp.FSharp.Advanced;

namespace SharpLab.Server.Compilation {
    public class Compiler : ICompiler {
        public async Task<bool> TryCompileToStreamAsync(MemoryStream assemblyStream, MemoryStream symbolStream, IWorkSession session, IList<Diagnostic> diagnostics, CancellationToken cancellationToken) {
            if (session.IsFSharp())
                return await TryCompileFSharpToStreamAsync(assemblyStream, session, diagnostics, cancellationToken);

            var compilation = await session.Roslyn.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var emitResult = compilation.Emit(assemblyStream, pdbStream: symbolStream);
            if (!emitResult.Success) {
                foreach (var diagnostic in emitResult.Diagnostics) {
                    diagnostics.Add(diagnostic);
                }
                return false;
            }
            return true;
        }

        private async Task<bool> TryCompileFSharpToStreamAsync(MemoryStream assemblyStream, IWorkSession session, IList<Diagnostic> diagnostics, CancellationToken cancellationToken) {
            var fsharp = session.FSharp();

            // GetLastParseResults are guaranteed to be available here as MirrorSharp's SlowUpdate does the parse
            var parsed = fsharp.GetLastParseResults();
            using (var virtualAssemblyFile = FSharpFileSystem.RegisterVirtualFile(assemblyStream)) {
                var compiled = await FSharpAsync.StartAsTask(fsharp.Checker.Compile(
                    // ReSharper disable once PossibleNullReferenceException
                    FSharpList<Ast.ParsedInput>.Cons(parsed.ParseTree.Value, FSharpList<Ast.ParsedInput>.Empty),
                    "_", virtualAssemblyFile.Name,
                    fsharp.AssemblyReferencePathsAsFSharpList,
                    pdbFile: null,
                    executable: false,//fsharp.ProjectOptions.OtherOptions.Contains("--target:exe"),
                    noframework: true
                ), null, cancellationToken).ConfigureAwait(false);
                foreach (var error in compiled.Item1) {
                    // no reason to add warnings as check would have added them anyways
                    if (error.Severity.Tag == FSharpErrorSeverity.Tags.Error)
                        diagnostics.Add(fsharp.ConvertToDiagnostic(error));
                }
                return virtualAssemblyFile.Stream.Length > 0;
            }
        }
    }
}
