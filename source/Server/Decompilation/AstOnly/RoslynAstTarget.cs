using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MirrorSharp.Advanced;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation.AstOnly {
    public class RoslynAstTarget : IAstTarget {
        public async Task<object> GetAstAsync(IWorkSession session, CancellationToken cancellationToken) {
            var document = session.Roslyn.Project.Documents.Single();
            return await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        }
        
        public void SerializeAst(object ast, IFastJsonWriter writer) {
            writer.WriteStartArray();
            SerializeNode((SyntaxNode)ast, writer);
            writer.WriteEndArray();
        }

        private void SerializeNode(SyntaxNode node, IFastJsonWriter writer, string specialParentPropertyName = null) {
            writer.WriteStartObject();
            writer.WriteProperty("type", "node");
            writer.WriteProperty("kind", RoslynSyntaxHelper.GetKindName(node.RawKind));
            var parentPropertyName = specialParentPropertyName ?? RoslynSyntaxHelper.GetParentPropertyName(node);
            if (parentPropertyName != null)
                writer.WriteProperty("property", parentPropertyName);
            SerializeSpanProperty(node.FullSpan, writer);
            writer.WritePropertyStartArray("children");
            foreach (var child in node.ChildNodesAndTokens()) {
                if (child.IsNode) {
                    SerializeNode(child.AsNode(), writer);
                }
                else {
                    SerializeToken(child.AsToken(), writer);
                }
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private void SerializeToken(SyntaxToken token, IFastJsonWriter writer) {
            writer.WriteStartObject();
            writer.WriteProperty("type", "token");
            writer.WriteProperty("kind", RoslynSyntaxHelper.GetKindName(token.RawKind));
            var parentPropertyName = RoslynSyntaxHelper.GetParentPropertyName(token);
            if (parentPropertyName != null)
                writer.WriteProperty("property", parentPropertyName);

            SerializeSpanProperty(token.FullSpan, writer);

            if (token.HasLeadingTrivia || token.HasTrailingTrivia) {
                writer.WritePropertyStartArray("children");
                foreach (var trivia in token.LeadingTrivia) {
                    SerializeTrivia(trivia, writer);
                }
                writer.WriteStartObject();
                writer.WriteProperty("type", "value");
                writer.WriteProperty("value", token.ValueText);
                SerializeSpanProperty(token.Span, writer);
                writer.WriteEndObject();
                foreach (var trivia in token.TrailingTrivia) {
                    SerializeTrivia(trivia, writer);
                }
                writer.WriteEndArray();
            }
            else {
                writer.WriteProperty("value", token.ToString());
            }
            writer.WriteEndObject();
        }

        private void SerializeTrivia(SyntaxTrivia trivia, IFastJsonWriter writer) {
            writer.WriteStartObject();
            writer.WriteProperty("type", "trivia");
            writer.WriteProperty("kind", RoslynSyntaxHelper.GetKindName(trivia.RawKind));
            SerializeSpanProperty(trivia.FullSpan, writer);
            if (trivia.HasStructure) {
                writer.WritePropertyStartArray("children");
                SerializeNode(trivia.GetStructure(), writer, "Structure");
                writer.WriteEndArray();
            }
            else {
                writer.WriteProperty("value", trivia.ToString());
            }
            writer.WriteEndObject();
        }

        private void SerializeSpanProperty(TextSpan span, IFastJsonWriter writer) {
            writer.WritePropertyName("range");
            using (var stringWriter = writer.OpenString()) {
                stringWriter.Write(span.Start);
                stringWriter.Write(":");
                stringWriter.Write(span.End);
            }
        }

        public IReadOnlyCollection<string> SupportedLanguageNames { get; } = new[] {
            LanguageNames.CSharp,
            LanguageNames.VisualBasic
        };
    }
}
