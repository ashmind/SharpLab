using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpLab.Server.Explanation.Internal {
    using static SyntaxFactory;

    public class SimplifyingCodeDisplayRewriter : CSharpSyntaxRewriter {
        public static SimplifyingCodeDisplayRewriter Default { get; } = new SimplifyingCodeDisplayRewriter();

        private static readonly SyntaxTrivia Ellipsis = Trivia(SkippedTokensTrivia(
            TokenList(BadToken(TriviaList(), "…", TriviaList()))
        ));
        // avoiding array allocations:
        private static readonly SyntaxTriviaList EllipsisTriviaList = TriviaList(Ellipsis);
        private static readonly SyntaxTriviaList EllipsisWithSpacesTriviaList = TriviaList(Space, Ellipsis, Space);

        public override SyntaxNode VisitBlock(BlockSyntax node) {
            if (node.Statements.Count == 0)
                return node;

            // replaces a block with "{ … }"
            return Block(
                node.OpenBraceToken.WithTrailingTrivia(EllipsisWithSpacesTriviaList),
                List<StatementSyntax>(),
                node.CloseBraceToken.WithLeadingTrivia(TriviaList())
            );
        }

        public override SyntaxNode VisitParameterList(ParameterListSyntax node) {
            if (node.Parameters.Count == 0)
                return node;

            // replaces parameter list with "(…)"
            return ParameterList(
                node.OpenParenToken.WithTrailingTrivia(EllipsisTriviaList),
                SeparatedList<ParameterSyntax>(),
                node.CloseParenToken.WithLeadingTrivia(TriviaList())
            );
        }
    }
}
