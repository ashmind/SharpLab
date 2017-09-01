using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AshMind.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.IO;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;
using SharpLab.Server.Compilation;
using SharpLab.Server.Decompilation;
using SharpLab.Server.Decompilation.AstOnly;
using SharpLab.Server.Execution;

namespace SharpLab.Server.MirrorSharp {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class SlowUpdate : ISlowUpdateExtension {
        private readonly ICompiler _compiler;
        private readonly IReadOnlyDictionary<string, IDecompiler> _decompilers;
        private readonly IReadOnlyDictionary<string, IAstTarget> _astTargets;
        private readonly IExecutor _executor;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public SlowUpdate(
            ICompiler compiler,
            IReadOnlyCollection<IDecompiler> decompilers,
            IReadOnlyCollection<IAstTarget> astTargets,
            IExecutor executor,
            RecyclableMemoryStreamManager memoryStreamManager
        ) {
            _compiler = compiler;
            _decompilers = decompilers.ToDictionary(d => d.LanguageName);
            _astTargets = astTargets
                .SelectMany(t => t.SupportedLanguageNames.Select(n => (target: t, languageName: n)))
                .ToDictionary(x => x.languageName, x => x.target);
            _executor = executor;
            _memoryStreamManager = memoryStreamManager;
        }

        public async Task<object> ProcessAsync(IWorkSession session, IList<Diagnostic> diagnostics, CancellationToken cancellationToken) {
            var targetName = session.GetTargetName();
            if (targetName == TargetNames.Ast) {
                var astTarget = _astTargets.GetValueOrDefault(session.LanguageName);
                return await astTarget.GetAstAsync(session, cancellationToken).ConfigureAwait(false);
            }

            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                return null;

            if (targetName != TargetNames.Run && !_decompilers.ContainsKey(targetName))
                throw new NotSupportedException($"Target '{targetName}' is not (yet?) supported by this branch.");

            MemoryStream assemblyStream = null;
            MemoryStream symbolStream = null;
            try {
                assemblyStream = _memoryStreamManager.GetStream();
                if (targetName == TargetNames.Run)
                    symbolStream = _memoryStreamManager.GetStream();

                if (!await _compiler.TryCompileToStreamAsync(assemblyStream, symbolStream, session, diagnostics, cancellationToken).ConfigureAwait(false)) {
                    assemblyStream.Dispose();
                    symbolStream?.Dispose();
                    return null;
                }
                assemblyStream.Seek(0, SeekOrigin.Begin);
                symbolStream?.Seek(0, SeekOrigin.Begin);
                if (targetName == TargetNames.Run)
                    return _executor.Execute(assemblyStream, symbolStream, session);

                // it's fine not to Dispose() here -- MirrorSharp will dispose it after calling WriteResult()
                return assemblyStream;
            }
            catch {
                assemblyStream?.Dispose();
                symbolStream?.Dispose();
                throw;
            }
        }

        public void WriteResult(IFastJsonWriter writer, object result, IWorkSession session) {
            if (result == null) {
                writer.WriteValue((string)null);
                return;
            }

            var targetName = session.GetTargetName();
            if (targetName == TargetNames.Ast) {
                var astTarget = _astTargets.GetValueOrDefault(session.LanguageName);
                astTarget.SerializeAst(result, writer, session);
                return;
            }

            if (targetName == TargetNames.Run) {
                _executor.Serialize((ExecutionResult)result, writer);
                return;
            }

            var decompiler = _decompilers[targetName];
            using (var stream = (Stream)result)
            using (var stringWriter = writer.OpenString()) {
                decompiler.Decompile(stream, stringWriter);
            }
        }
    }
}