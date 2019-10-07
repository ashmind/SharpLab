using Microsoft.CodeAnalysis;

namespace SharpLab.Server.Decompilation.AstOnly {
    public class RoslynAst {
        public RoslynAst(SyntaxNode syntaxRoot, SemanticModel semanticModel) {
            SyntaxRoot = syntaxRoot;
            SemanticModel = semanticModel;
        }

        public SyntaxNode SyntaxRoot { get; }
        public SemanticModel SemanticModel { get; }
    }
}
