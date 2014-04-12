using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp.RuntimeBinder;

namespace TryRoslyn.Web.Internal {
    public class CompilationService {
        private readonly IDecompiler _decompiler;

        private static readonly MetadataReference[] References = {
            new MetadataFileReference(typeof(object).Assembly.Location),
            new MetadataFileReference(typeof(Uri).Assembly.Location),
            new MetadataFileReference(typeof(DynamicAttribute).Assembly.Location),
            new MetadataFileReference(typeof(Binder).Assembly.Location),
        };

        public CompilationService(IDecompiler decompiler) {
            _decompiler = decompiler;
        }

        public ProcessingResult Process(string code) {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var stream = new MemoryStream();
            var emitResult = CSharpCompilation.Create("Test")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(References)
                .AddSyntaxTrees(syntaxTree)
                .Emit(stream);

            if (!emitResult.Success)
                return new ProcessingResult(null, emitResult.Diagnostics);

            stream.Seek(0, SeekOrigin.Begin);

            var resultWriter = new StringWriter();
            _decompiler.Decompile(stream, resultWriter);
            return new ProcessingResult(resultWriter.ToString(), emitResult.Diagnostics);
        }
    }
}