using System.Collections.Generic;
using System.IO;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.VB;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Visitors;
using Microsoft.CodeAnalysis;
using TryRoslyn.Core.Decompilation.Support;
using AstNode = ICSharpCode.NRefactory.CSharp.AstNode;

namespace TryRoslyn.Core.Decompilation {
    public class VisualBasicDecompiler : AstDecompiler {
        protected override void WriteResult(TextWriter writer, IEnumerable<AstNode> ast, DecompilerContext context) {
            var converter = new CSharpToVBConverterVisitor(new ILSpyEnvironmentProvider());
            var visitor = new OutputVisitor(
                new VBTextOutputFormatter(new CustomizableIndentPlainTextOutput(writer) {
                    IndentationString = "    "
                }),
                new VBFormattingOptions()
            );
            foreach (var node in ast) {
                node.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
                var converted = node.AcceptVisitor(converter, null);
                converted.AcceptVisitor(visitor, null);
            }
        }

        public override string LanguageName => LanguageNames.VisualBasic;
    }
}