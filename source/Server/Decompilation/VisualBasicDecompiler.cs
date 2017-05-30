using System.Collections.Generic;
using System.IO;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.VB;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Visitors;
using Microsoft.CodeAnalysis;
using SharpLab.Server.Decompilation.Internal;
using AstNode = ICSharpCode.NRefactory.CSharp.AstNode;

namespace SharpLab.Server.Decompilation {
    public class VisualBasicDecompiler : AstBasedDecompiler {
        protected override void WriteResult(TextWriter writer, AstNode ast, DecompilerContext context) {
            var converter = new CSharpToVBConverterVisitor(new ILSpyEnvironmentProvider());
            var visitor = new OutputVisitor(
                new VBTextOutputFormatter(new CustomizableIndentPlainTextOutput(writer) {
                    IndentationString = "    "
                }),
                new VBFormattingOptions()
            );
            ast.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            var converted = ast.AcceptVisitor(converter, null);
            converted.AcceptVisitor(visitor, null);
        }

        public override string LanguageName => LanguageNames.VisualBasic;
    }
}