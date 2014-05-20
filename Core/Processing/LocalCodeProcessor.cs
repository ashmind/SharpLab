using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp.RuntimeBinder;
using TryRoslyn.Core.Processing.RoslynSupport;

namespace TryRoslyn.Core.Processing {
    [ThreadSafe]
    public class LocalCodeProcessor : ICodeProcessor {
        private readonly IDecompiler _decompiler;
        private readonly IRoslynAbstraction _roslynAbstraction;

        private readonly MetadataReference[] _references;
        
        public LocalCodeProcessor(IDecompiler decompiler, IRoslynAbstraction roslynAbstraction) {
            _decompiler = decompiler;
            _roslynAbstraction = roslynAbstraction;

            _references = new MetadataReference[] {
                _roslynAbstraction.NewMetadataFileReference(typeof(object).Assembly.Location),
                _roslynAbstraction.NewMetadataFileReference(typeof(Uri).Assembly.Location),
                _roslynAbstraction.NewMetadataFileReference(typeof(DynamicAttribute).Assembly.Location),
                _roslynAbstraction.NewMetadataFileReference(typeof(Binder).Assembly.Location)
            };
        }

        public ProcessingResult Process(string code, bool scriptMode, bool optimizations) {
            var kind = scriptMode ? SourceCodeKind.Script : SourceCodeKind.Regular;
            var syntaxTree = CSharpSyntaxTree.ParseText(
                code, options: new CSharpParseOptions(_roslynAbstraction.GetMaxLanguageVersion(), kind: kind)
            );

            var stream = new MemoryStream();
            var emitResult = CSharpCompilation.Create("Test")
                .WithOptions(_roslynAbstraction
                    .NewCSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithAllowUnsafe(enabled: true)
                    .WithOptimizations(optimizations))
                .AddReferences(_references)
                .AddSyntaxTrees(syntaxTree)
                .Emit(stream);

            if (!emitResult.Success)
                return new ProcessingResult(null, emitResult.Diagnostics.Select(d => new SerializableDiagnostic(d)));

            stream.Seek(0, SeekOrigin.Begin);

            var resultWriter = new StringWriter();
            _decompiler.Decompile(stream, resultWriter);
            return new ProcessingResult(
                resultWriter.ToString(),
                emitResult.Diagnostics.Select(d => new SerializableDiagnostic(d))
            );
        }

        #region IDisposable Members

        void IDisposable.Dispose() {
        }

        #endregion
    }
}