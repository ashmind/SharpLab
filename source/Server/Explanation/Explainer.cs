using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MirrorSharp.Advanced;
using SourcePath.Roslyn;
using SharpLab.Server.Explanation.Internal;

namespace SharpLab.Server.Explanation {
    public class Explainer : IExplainer {
        private readonly ISyntaxExplanationProvider _explanationProvider;
        private readonly IRoslynCSharpSyntaxQueryExecutor _syntaxPathExecutor;

        public Explainer(ISyntaxExplanationProvider explanationProvider, IRoslynCSharpSyntaxQueryExecutor syntaxPathExecutor) {
            _explanationProvider = explanationProvider;
            _syntaxPathExecutor = syntaxPathExecutor;
        }

        public async Task<ExplanationResult> ExplainAsync(object ast, IWorkSession session, CancellationToken cancellationToken) {
            if (session.LanguageName != LanguageNames.CSharp)
                throw new NotSupportedException("Explain mode is experimental and only available for C# at the moment.");

            var explanations = await _explanationProvider.GetExplanationsAsync(cancellationToken).ConfigureAwait(false);
            return new ExplanationResult(MapExplanations(ast, explanations));
        }

        private IEnumerable<(SyntaxNodeOrToken, SyntaxExplanation)> MapExplanations(object ast, IReadOnlyCollection<SyntaxExplanation> explanations) {
            var tree = (CSharpSyntaxNode)ast;
            var results = new List<(SyntaxNode, SyntaxExplanation)>();
            foreach (var explanation in explanations) {
                var match = _syntaxPathExecutor.QueryAll(tree, explanation.Path).FirstOrDefault();
                if (match != default(SyntaxNodeOrToken))
                    yield return (match, explanation);
            }
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
