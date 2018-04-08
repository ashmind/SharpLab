using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MirrorSharp.Advanced;
using SharpLab.Server.Explanation.Internal;

namespace SharpLab.Server.Explanation {
    public class Explainer : IExplainer {
        private readonly ISyntaxExplanationProvider _explanationProvider;

        public Explainer(ISyntaxExplanationProvider explanationProvider) {
            _explanationProvider = explanationProvider;
        }

        public async Task<ExplanationResult> ExplainAsync(object ast, IWorkSession session, CancellationToken cancellationToken) {
            if (session.LanguageName != LanguageNames.CSharp)
                throw new NotSupportedException("Explain mode is experimental and only available for C# at the moment.");

            var explanations = await _explanationProvider.GetExplanationsAsync(cancellationToken).ConfigureAwait(false);
            return new ExplanationResult(MapExplanations(ast, explanations));
        }

        private IEnumerable<(SyntaxNodeOrToken, SyntaxExplanation)> MapExplanations(object ast, IReadOnlyDictionary<SyntaxKind, SyntaxExplanation> explanations) {
            var seen = new HashSet<SyntaxExplanation>();
            var tree = (CSharpSyntaxNode)ast;
            var results = new List<(SyntaxNode, SyntaxExplanation)>();
            foreach (var descendant in tree.DescendantNodesAndTokensAndSelf()) {
                if (!explanations.TryGetValue(descendant.Kind(), out var explanation) || !seen.Add(explanation))
                    continue;
                yield return (explanation.GetBestFragment(descendant), explanation);
            }
        }

        public void Serialize(ExplanationResult result, IFastJsonWriter writer) {
            writer.WriteStartArray();
            foreach (var (fragment, explanation) in result.Explanations) {
                writer.WriteStartObject();
                writer.WritePropertyName("code");
                using (var code = writer.OpenString()) {
                    fragment.WriteTo(code);
                }
                writer.WriteProperty("name", explanation.Name);
                writer.WriteProperty("text", explanation.Text);
                writer.WriteProperty("link", explanation.Link);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}
