using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MirrorSharp.Advanced;
using SharpLab.Server.Explanation.Internal;
using SourcePath;

namespace SharpLab.Server.Explanation {
    public class Explainer : IExplainer {
        private readonly ISyntaxExplanationProvider _explanationProvider;

        public Explainer(ISyntaxExplanationProvider explanationProvider) {
            _explanationProvider = explanationProvider;
        }

        public async Task<ExplanationResult> ExplainAsync(object ast, IWorkSession session, CancellationToken cancellationToken) {
            if (session.LanguageName != LanguageNames.CSharp)
                throw new NotSupportedException("Explain mode is only available for C# at the moment.");

            var explanations = await _explanationProvider.GetExplanationsAsync(cancellationToken).ConfigureAwait(false);
            return new ExplanationResult(MapExplanations(ast, explanations));
        }

        private IEnumerable<(SyntaxNodeOrToken, SyntaxExplanation)> MapExplanations(object ast, IReadOnlyCollection<SyntaxExplanation> explanations) {
            var seen = new HashSet<SyntaxExplanation>();
            var tree = (CSharpSyntaxNode)ast;
            var results = new List<(SyntaxNode, SyntaxExplanation)>();
            foreach (var descendant in tree.DescendantNodesAndTokensAndSelf()) {
                foreach (var explanation in explanations) {
                    if (seen.Contains(explanation))
                        continue;

                    var matched = Match(descendant, explanation);
                    if (matched == null)
                        continue;

                    seen.Add(explanation);
                    yield return (matched.Value, explanation);
                }
            }
        }

        private SyntaxNodeOrToken? Match(SyntaxNodeOrToken descendant, SyntaxExplanation explanation) {
            var path = explanation.Path;
            if (path is SourcePathSequence<SyntaxNodeOrToken> sequence) {
                var segment = sequence.Segments[0];
                if (segment.Kind is CSharpExplanationPathDialect.StarNodeKind) {
                    if (segment.Filter.Matches(descendant, SourcePathAxis.Self))
                        return descendant.Parent;
                    return null;
                }
            }

            if (path.Matches(descendant, SourcePathAxis.Self))
                return descendant;
            return null;
        }

        public void Serialize(ExplanationResult result, IFastJsonWriter writer) {
            writer.WriteStartArray();
            foreach (var (fragment, explanation) in result.Explanations) {
                writer.WriteStartObject();
                writer.WritePropertyName("code");
                using (var code = writer.OpenString()) {
                    SerializeFragment(fragment, code);
                }
                writer.WriteProperty("name", explanation.Name);
                writer.WriteProperty("text", explanation.Text);
                writer.WriteProperty("link", explanation.Link);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        private void SerializeFragment(SyntaxNodeOrToken fragment, TextWriter code) {
            if (fragment.IsToken) {
                fragment.WriteTo(code);
                return;
            }

            var simplified = SimplifyingCodeDisplayRewriter.Default.Visit(fragment.AsNode());
            simplified.WriteTo(code);
        }
    }
}
