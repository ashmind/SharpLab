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
using SharpLab.Server.Explanation;

namespace SharpLab.Server.MirrorSharp {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class SlowUpdate : ISlowUpdateExtension {
        private readonly ICompiler _compiler;
        private readonly IReadOnlyDictionary<string, IDecompiler> _decompilers;
        private readonly IReadOnlyDictionary<string, IAstTarget> _astTargets;
        private readonly IExecutor _executor;
        private readonly IExplainer _explainer;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public SlowUpdate(
            ICompiler compiler,
            IReadOnlyCollection<IDecompiler> decompilers,
            IReadOnlyCollection<IAstTarget> astTargets,
            IExecutor executor,
            IExplainer explainer,
            RecyclableMemoryStreamManager memoryStreamManager
        ) {
            _compiler = compiler;
            _decompilers = decompilers.ToDictionary(d => d.LanguageName);
            _astTargets = astTargets
                .SelectMany(t => t.SupportedLanguageNames.Select(n => (target: t, languageName: n)))
                .ToDictionary(x => x.languageName, x => x.target);
            _executor = executor;
            _memoryStreamManager = memoryStreamManager;
            _explainer = explainer;
        }

        public async Task<object?> ProcessAsync(IWorkSession session, IList<Diagnostic> diagnostics, CancellationToken cancellationToken) {
            var targetName = GetAndEnsureTargetName(session);

            if (targetName == TargetNames.Ast || targetName == TargetNames.Explain) {
                var astTarget = _astTargets[session.LanguageName];
                var ast = await astTarget.GetAstAsync(session, cancellationToken).ConfigureAwait(false);
                if (targetName == TargetNames.Explain)
                    return await _explainer.ExplainAsync(ast, session, cancellationToken).ConfigureAwait(false);
                return ast;
            }

            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                return null;

            if (targetName == LanguageNames.VisualBasic)
                return VisualBasicNotAvailable;

            if (targetName != TargetNames.Run && targetName != TargetNames.Verify && !_decompilers.ContainsKey(targetName))
                throw new NotSupportedException($"Target '{targetName}' is not (yet?) supported by this branch.");

            MemoryStream? assemblyStream = null;
            MemoryStream? symbolStream = null;
            try {
                assemblyStream = _memoryStreamManager.GetStream();
                if (targetName == TargetNames.Run || targetName == TargetNames.IL)
                    symbolStream = _memoryStreamManager.GetStream();

                var compiled = await _compiler.TryCompileToStreamAsync(assemblyStream, symbolStream, session, diagnostics, cancellationToken).ConfigureAwait(false);
                if (!compiled.assembly) {
                    assemblyStream.Dispose();
                    symbolStream?.Dispose();
                    return null;
                }

                if (targetName == TargetNames.Verify) {
                    assemblyStream.Dispose();
                    symbolStream?.Dispose();
                    return "✔️ Compilation completed.";
                }

                assemblyStream.Seek(0, SeekOrigin.Begin);
                symbolStream?.Seek(0, SeekOrigin.Begin);
                var streams = new CompilationStreamPair(assemblyStream, compiled.symbols ? symbolStream : null);
                if (targetName == TargetNames.Run)
                    return _executor.Execute(streams, session);

                // it's fine not to Dispose() here -- MirrorSharp will dispose it after calling WriteResult()
                return streams;
            }
            catch {
                assemblyStream?.Dispose();
                symbolStream?.Dispose();
                throw;
            }
        }

        public void WriteResult(IFastJsonWriter writer, object result, IWorkSession session) {
            if (result == null) {
                writer.WriteValue((string?)null);
                return;
            }

            if (result is string s) {
                writer.WriteValue(s);
                return;
            }

            var targetName = GetAndEnsureTargetName(session);
            if (targetName == TargetNames.Ast) {
                var astTarget = _astTargets[session.LanguageName];
                astTarget.SerializeAst(result, writer, session);
                return;
            }

            if (targetName == TargetNames.Explain) {
                _explainer.Serialize((ExplanationResult)result, writer);
                return;
            }

            if (targetName == TargetNames.Run) {
                _executor.Serialize((ExecutionResult)result, writer);
                return;
            }

            var decompiler = _decompilers[targetName];
            using (var streams = (CompilationStreamPair)result)
            using (var stringWriter = writer.OpenString()) {
                decompiler.Decompile(streams, stringWriter);
            }
        }

        private const string VisualBasicNotAvailable =
            "' Unfortunately, Visual Basic decompilation is no longer supported.\r\n" +
            "' \r\n" +
            "' All decompilation in SharpLab is provided by ILSpy, and latest ILSpy does not suport VB.\r\n" +
            "' If you are interested in VB, please discuss or contribute at https://github.com/icsharpcode/ILSpy.";

        private string GetAndEnsureTargetName(IWorkSession session) {
            var targetName = session.GetTargetName();
            if (targetName == null)
                throw new InvalidOperationException("Target is not set on the session (timing issue?). Please try reloading.");
            return targetName;
        }
    }
}