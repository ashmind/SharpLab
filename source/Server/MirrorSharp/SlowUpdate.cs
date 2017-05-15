using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Control;
using Microsoft.IO;
using MirrorSharp.Advanced;
using MirrorSharp.FSharp.Advanced;
using TryRoslyn.Server.Decompilation;

namespace TryRoslyn.Server.MirrorSharp {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class SlowUpdate : ISlowUpdateExtension {
        private readonly IReadOnlyCollection<IDecompiler> _decompilers;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public SlowUpdate(IReadOnlyCollection<IDecompiler> decompilers, RecyclableMemoryStreamManager memoryStreamManager) {
            _decompilers = decompilers;
            _memoryStreamManager = memoryStreamManager;
        }

        public async Task<object> ProcessAsync(IWorkSession session, IList<Diagnostic> diagnostics, CancellationToken cancellationToken) {
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                return null;

            var targetLanguageName = session.GetTargetLanguageName();
            var decompiler = _decompilers.FirstOrDefault(d => d.LanguageName == targetLanguageName);
            if (decompiler == null)
                throw new NotSupportedException($"Target '{targetLanguageName}' is not (yet?) supported by this branch.");

            using (var stream = _memoryStreamManager.GetStream()) {
                if (!await TryEmitAssemblyAsync(stream, session, diagnostics, cancellationToken).ConfigureAwait(false))
                    return null;

                stream.Seek(0, SeekOrigin.Begin);

                var resultWriter = new StringWriter();
                decompiler.Decompile(stream, resultWriter);
                return resultWriter.ToString();
            }
        }

        private async Task<bool> TryEmitAssemblyAsync(MemoryStream assemblyStream, IWorkSession session, IList<Diagnostic> diagnostics, CancellationToken cancellationToken) {
            if (session.IsFSharp())
                return await TryEmitFSharpAssemblyAsync(assemblyStream, session, diagnostics, cancellationToken);
            
            var compilation = await session.Roslyn.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var emitResult = compilation.Emit(assemblyStream);
            if (!emitResult.Success) {
                foreach (var diagnostic in emitResult.Diagnostics) {
                    diagnostics.Add(diagnostic);
                }
                return false;
            }
            return true;
        }

        private async Task<bool> TryEmitFSharpAssemblyAsync(MemoryStream assemblyStream, IWorkSession session, IList<Diagnostic> diagnostics, CancellationToken cancellationToken) {
            var fsharp = session.FSharp();

            // GetLastParseResults are guaranteed to be available here as MirrorSharp's SlowUpdate does the parse
            var parsed = fsharp.GetLastParseResults();
            using (var virtualAssemblyFile = FSharpFileSystem.RegisterVirtualFile(assemblyStream)) {
                var compiled = await FSharpAsync.StartAsTask(fsharp.Checker.Compile(
                    FSharpList<Ast.ParsedInput>.Cons(parsed.ParseTree.Value, FSharpList<Ast.ParsedInput>.Empty),
                    "_", virtualAssemblyFile.Name,
                    fsharp.AssemblyReferencePathsAsFSharpList,
                    pdbFile: null, 
                    executable: false,
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

        public void WriteResult(IFastJsonWriter writer, object result) {
            if (result != null)
                writer.WriteProperty("decompiled", (string)result);
        }
    }
}