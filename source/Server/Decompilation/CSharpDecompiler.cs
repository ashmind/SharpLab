using System.Collections.Generic;
using System.IO;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.CSharp;
using Microsoft.CodeAnalysis;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    public class CSharpDecompiler : AstBasedDecompiler {
        protected override void WriteResult(TextWriter writer, AstBuilder ast) {
            ast.GenerateCode(new CustomizableIndentPlainTextOutput(writer) {
                IndentationString = "    "
            });
        }

        public override string LanguageName => LanguageNames.CSharp;
    }
}