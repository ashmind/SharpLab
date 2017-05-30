using System.Collections.Generic;
using System.IO;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Microsoft.CodeAnalysis;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    public class CSharpDecompiler : AstBasedDecompiler {
        protected override void WriteResult(TextWriter writer, AstNode ast, DecompilerContext context) {
            var visitor = new DecompiledPseudoCSharpOutputVisitor(
                new TextWriterOutputFormatter(writer) {
                    IndentationString = "    "
                },
                context.Settings.CSharpFormattingOptions
            );

            ast.AcceptVisitor(visitor);
        }

        public override string LanguageName => LanguageNames.CSharp;
    }
}