using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MirrorSharp.Advanced;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation.AstOnly {
    public partial class RoslynAstTarget : IAstTarget {
        private readonly IRoslynOperationPropertySerializer _operationPropertySerializer;

        public RoslynAstTarget(IRoslynOperationPropertySerializer operationPropertySerializer) {
            _operationPropertySerializer = operationPropertySerializer;
        }

        public async Task<object> GetAstAsync(IWorkSession session, CancellationToken cancellationToken) {
            var document = session.Roslyn.Project.Documents.Single();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            return new RoslynAst(syntaxRoot, semanticModel);
        }

        public void SerializeAst(object ast, IFastJsonWriter writer, IWorkSession session) {
            SerializeAst((RoslynAst)ast, writer, session);
        }

        private void SerializeAst(RoslynAst ast, IFastJsonWriter writer, IWorkSession session) {
            writer.WriteStartArray();
            SerializeNode(ast.SyntaxRoot, ast.SemanticModel, writer);
            writer.WriteEndArray();
        }

        private void SerializeNode(SyntaxNode node, SemanticModel semanticModel, IFastJsonWriter writer, string? specialParentPropertyName = null) {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            writer.WriteStartObject();
            writer.WriteProperty("type", "node");
            writer.WriteProperty("kind", RoslynSyntaxHelper.GetKindName(node.RawKind));
            var parentPropertyName = specialParentPropertyName ?? RoslynSyntaxHelper.GetParentPropertyName(node);
            if (parentPropertyName != null)
                writer.WriteProperty("property", parentPropertyName);
            SerializeSpanProperty(node.FullSpan, writer);

            writer.WritePropertyStartArray("children");
            var operation = semanticModel.GetOperation(node);
            if (operation != null)
                SerializeOperation(operation, writer);

            foreach (var child in node.ChildNodesAndTokens()) {
                if (child.IsNode) {
                    SerializeNode(child.AsNode(), semanticModel, writer);
                }
                else {
                    SerializeToken(child.AsToken(), semanticModel, writer);
                }
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        private void SerializeToken(SyntaxToken token, SemanticModel semanticModel, IFastJsonWriter writer) {
            RuntimeHelpers.EnsureSufficientExecutionStack();
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
                    SerializeTrivia(trivia, semanticModel, writer);
                }
                writer.WriteStartObject();
                writer.WriteProperty("type", "value");
                writer.WriteProperty("value", token.ValueText);
                SerializeSpanProperty(token.Span, writer);
                writer.WriteEndObject();
                foreach (var trivia in token.TrailingTrivia) {
                    SerializeTrivia(trivia, semanticModel, writer);
                }
                writer.WriteEndArray();
            }
            else {
                writer.WriteProperty("value", token.ToString());
            }
            writer.WriteEndObject();
        }

        private void SerializeTrivia(SyntaxTrivia trivia, SemanticModel semanticModel, IFastJsonWriter writer) {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            writer.WriteStartObject();
            writer.WriteProperty("type", "trivia");
            writer.WriteProperty("kind", RoslynSyntaxHelper.GetKindName(trivia.RawKind));
            SerializeSpanProperty(trivia.FullSpan, writer);
            if (trivia.HasStructure) {
                writer.WritePropertyStartArray("children");
                SerializeNode(trivia.GetStructure(), semanticModel, writer, "Structure");
                writer.WriteEndArray();
            }
            else {
                writer.WriteProperty("value", trivia.ToString());
            }
            writer.WriteEndObject();
        }

        private void SerializeSpanProperty(TextSpan span, IFastJsonWriter writer) {
            writer.WritePropertyName("range");
            writer.WriteValueFromParts(span.Start, '-', span.End);
        }

        //private void SerializeSymbol(ISymbol symbol, IFastJsonWriter writer, string relationToParent) {
        //    RuntimeHelpers.EnsureSufficientExecutionStack();
        //    writer.WriteStartObject();
        //    writer.WriteProperty("type", "symbol");
        //    writer.WriteProperty("property", relationToParent);
        //    writer.WriteProperty("kind", symbol.Kind.ToString());
        //    writer.WritePropertyStartArray("children");
        //    writer.WriteStartObject();
        //    writer.WriteProperty("type", "property");
        //    writer.WriteProperty("property", "Name");
        //    writer.WriteProperty("value", symbol.Name);
        //    writer.WriteEndObject();
        //    writer.WriteEndArray();
        //    writer.WriteEndObject();
        //}

        private void SerializeOperation(IOperation operation, IFastJsonWriter writer) {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            writer.WriteStartObject();
            writer.WriteProperty("type", "operation");
            writer.WriteProperty("property", "Operation");
            writer.WriteProperty("kind", operation.Kind.ToString());

            writer.WritePropertyStartObject("properties");
            _operationPropertySerializer.SerializeProperties(operation, writer);
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        public IReadOnlyCollection<string> SupportedLanguageNames { get; } = new[] {
            LanguageNames.CSharp,
            LanguageNames.VisualBasic
        };
    }
}
