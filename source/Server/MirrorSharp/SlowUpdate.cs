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
using SharpLab.Server.Compilation;
using SharpLab.Server.Decompilation;
using SharpLab.Server.Decompilation.AstOnly;

namespace SharpLab.Server.MirrorSharp {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class SlowUpdate : ISlowUpdateExtension {
        private const string AstTargetName = "AST";

        private readonly ICompiler _compiler;
        private readonly IReadOnlyDictionary<string, IDecompiler> _decompilers;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;
        private readonly IReadOnlyDictionary<string, IAstTarget> _astTargets;

        public SlowUpdate(
            ICompiler compiler,
            IReadOnlyCollection<IDecompiler> decompilers,
            RecyclableMemoryStreamManager memoryStreamManager,
            IReadOnlyCollection<IAstTarget> astTargets
        ) {
            _compiler = compiler;
            _decompilers = decompilers.ToDictionary(d => d.LanguageName);
            _memoryStreamManager = memoryStreamManager;
            _astTargets = astTargets
                .SelectMany(t => t.SupportedLanguageNames.Select(n => (target: t, languageName: n)))
                .ToDictionary(x => x.languageName, x => x.target);
        }

        public async Task<object> ProcessAsync(IWorkSession session, IList<Diagnostic> diagnostics, CancellationToken cancellationToken) {
            var targetName = session.GetTargetName();
            if (targetName == AstTargetName) {
                var astTarget = _astTargets.GetValueOrDefault(session.LanguageName);
                return await astTarget.GetAstAsync(session, cancellationToken).ConfigureAwait(false);
            }

            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                return null;

            var decompiler = _decompilers.GetValueOrDefault(targetName);
            if (decompiler == null)
                throw new NotSupportedException($"Target '{targetName}' is not (yet?) supported by this branch.");

            using (var stream = _memoryStreamManager.GetStream()) {
                if (!await _compiler.TryCompileToStreamAsync(stream, session, diagnostics, cancellationToken).ConfigureAwait(false))
                    return null;

                stream.Seek(0, SeekOrigin.Begin);

                var resultWriter = new StringWriter();
                decompiler.Decompile(stream, resultWriter);
                return resultWriter.ToString();
            }
        }

        public void WriteResult(IFastJsonWriter writer, object result, IWorkSession session) {
            if (result == null) {
                writer.WriteValue(null);
                return;
            }

            if (session.GetTargetName() == AstTargetName) {
                var astTarget = _astTargets.GetValueOrDefault(session.LanguageName);
                astTarget.SerializeAst(result, writer, session);
                return;
            }

            writer.WriteValue((string)result);
        }
    }
}