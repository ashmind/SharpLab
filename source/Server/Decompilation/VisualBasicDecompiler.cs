using System.IO;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.ILSpy.VB;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Visitors;
using Microsoft.CodeAnalysis;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    public class VisualBasicDecompiler : AstBasedDecompiler {
        protected override void WriteResult(TextWriter writer, AstBuilder ast) {
            ast.RunTransformations();

            var converter = new CSharpToVBConverterVisitor(new ILSpyEnvironmentProvider());
            var visitor = new OutputVisitor(
                new VBTextOutputFormatter(new CustomizableIndentPlainTextOutput(writer) {
                    IndentationString = "    "
                }),
                new VBFormattingOptions()
            );
            var converted = ast.SyntaxTree.AcceptVisitor(converter, null);
            converted.AcceptVisitor(visitor, null);
        }

        public override string LanguageName => LanguageNames.VisualBasic;
    }
}