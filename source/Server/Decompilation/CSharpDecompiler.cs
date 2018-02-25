using System.IO;
using ICSharpCode.Decompiler.Ast;
using Microsoft.CodeAnalysis;
using Mono.Cecil;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    public class CSharpDecompiler : AstBasedDecompiler {
        public CSharpDecompiler(IAssemblyResolver assemblyResolver) : base(assemblyResolver) {
        }

        protected override void WriteResult(TextWriter writer, AstBuilder ast) {
            ast.GenerateCode(new CustomizableIndentPlainTextOutput(writer) {
                IndentationString = "    "
            });
        }

        public override string LanguageName => LanguageNames.CSharp;
    }
}