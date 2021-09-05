using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FSharp.Compiler.Diagnostics;
using FSharp.Compiler.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using MirrorSharp.Advanced;
using MirrorSharp.FSharp.Advanced;
using MirrorSharp.IL.Advanced;
using Mobius.ILasm.Core;
using Mobius.ILasm.interfaces;
using Location = Mono.ILASM.Location;

namespace SharpLab.Server.Compilation {
    public class Compiler : ICompiler {
        private static readonly EmitOptions RoslynEmitOptions = new(
            // TODO: try out embedded
            debugInformationFormat: DebugInformationFormat.PortablePdb
        );

        public async Task<(bool assembly, bool symbols)> TryCompileToStreamAsync(MemoryStream assemblyStream,
            MemoryStream? symbolStream, IWorkSession session, IList<Diagnostic> diagnostics,
            CancellationToken cancellationToken) {
            if (session.IsFSharp()) {
                var compiled =
                    await TryCompileFSharpToStreamAsync(assemblyStream, session, diagnostics, cancellationToken)
                        .ConfigureAwait(false);
                return (compiled, false);
            }

            if (session.IsIL()) {
                var compiled = TryCompileILToStreamAsync(assemblyStream, session, diagnostics, cancellationToken);
                return (compiled, false);
            }

            #warning TODO: Revisit after https: //github.com/dotnet/docs/issues/14784
            var compilation =
                (await session.Roslyn.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!;
            var emitResult = compilation.Emit(assemblyStream, pdbStream: symbolStream, options: RoslynEmitOptions);
            if (!emitResult.Success) {
                foreach (var diagnostic in emitResult.Diagnostics) {
                    diagnostics.Add(diagnostic);
                }

                return (false, false);
            }

            return (true, true);
        }

        private async Task<bool> TryCompileFSharpToStreamAsync(MemoryStream assemblyStream, IWorkSession session,
            IList<Diagnostic> diagnostics, CancellationToken cancellationToken) {
            var fsharp = session.FSharp();

            // GetLastParseResults are guaranteed to be available here as MirrorSharp's SlowUpdate does the parse
            var parsed = fsharp.GetLastParseResults()!;
            using (var virtualAssemblyFile = FSharpFileSystem.RegisterVirtualFile(assemblyStream)) {
                var compiled = await FSharpAsync.StartAsTask(fsharp.Checker.Compile(
                    FSharpList<ParsedInput>.Cons(parsed.ParseTree, FSharpList<ParsedInput>.Empty),
                    "_", virtualAssemblyFile.Name,
                    fsharp.AssemblyReferencePathsAsFSharpList,
                    pdbFile: null,
                    executable: false, //fsharp.ProjectOptions.OtherOptions.Contains("--target:exe"),
                    noframework: true,
                    userOpName: null
                ), null, cancellationToken).ConfigureAwait(false);
                foreach (var diagnostic in compiled.Item1) {
                    // no reason to add warnings as check would have added them anyways
                    if (diagnostic.Severity.Tag == FSharpDiagnosticSeverity.Tags.Error)
                        diagnostics.Add(fsharp.ConvertToDiagnostic(diagnostic));
                }

                return virtualAssemblyFile.Stream.Length > 0;
            }
        }

        private bool TryCompileILToStreamAsync(MemoryStream assemblyStream, IWorkSession session,
            IList<Diagnostic> diagnostics, CancellationToken cancellationToken) {
            var cil = session.IL();

            var logger = new Logger();
            var driver = new Driver(logger, Driver.Target.Dll, false, false, false);
            var result = driver.Assemble(new [] {session.GetText() }, assemblyStream);
            assemblyStream.Seek(0, SeekOrigin.Begin);
            return result;
        }
    }

    public class Logger : ILogger {
        public void Info(string message) {
        }

        public void Error(string message) {
        }

        public void Error(Location location, string message) {
        }

        public void Warning(string message) {
        }

        public void Warning(Location location, string message) {
        }
    }
}