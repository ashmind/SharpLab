using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourcePath;
using SourcePath.Roslyn;

namespace SharpLab.Server.Explanation.Internal {
    using IRoslynNodeKind = ISourceNodeKind<RoslynNodeContext>;

    public class CSharpExplanationPathDialect : ISourcePathDialect<RoslynNodeContext> {
        private static readonly IReadOnlyDictionary<string, IRoslynNodeKind> NodeKinds =
            EnumerateNodeKinds().ToDictionary(k => k.ToPathString(), k => k);

        private static IEnumerable<IRoslynNodeKind> EnumerateNodeKinds() {
            // Normally Enum.GetValues does not require Distinct, but there are some branches of Roslyn
            // that (temporarily?) have duplicate values, e.g. see
            // https://github.com/dotnet/roslyn/blob/7ea6fa4d1871175f5ad5445677c0e0dd1a7a597c/src/Compilers/Core/Portable/Operations/OperationKind.cs#L209-L212
            IEnumerable<T> GetEnumValues<T>() where T: Enum
                => Enum.GetValues(typeof(T)).Cast<T>().Distinct();

            foreach (var syntaxKind in GetEnumValues<SyntaxKind>()) {
                yield return new RoslynCSharpNodeKind(syntaxKind.ToString(), new HashSet<SyntaxKind> { syntaxKind });
            }

            foreach (var symbolKind in GetEnumValues<SymbolKind>()) {
                yield return new SymbolNodeKind(symbolKind);
            }

            foreach (var operationKind in GetEnumValues<OperationKind>()) {
                yield return new OperationNodeKind(operationKind);
            }

            yield return IsVerbatimNodeKind.Default;
            yield return StarNodeKind.Default;
        }

        public SourcePathDialectSupports Supports { get; } = new SourcePathDialectSupports {
            TopLevelAxis = false,
            TopLevelSegments = false,
            AxisSelf = false,
            AxisDescendant = false,
            AxisParent = false,
            AxisAncestor = false
        };

        public IRoslynNodeKind ResolveNodeKind(string nodeKindString) {
            Argument.NotNullOrEmpty(nameof(nodeKindString), nodeKindString);

            if (!NodeKinds.TryGetValue(nodeKindString, out var nodeKind))
                return UnknownNodeKind.Default;

            return nodeKind;
        }

        public class StarNodeKind : IRoslynNodeKind {
            public static StarNodeKind Default { get; } = new StarNodeKind();

            public bool Matches(RoslynNodeContext context) => true;
            public string ToPathString() => "*";
        }

        public class IsVerbatimNodeKind : IRoslynNodeKind {
            public static IsVerbatimNodeKind Default { get; } = new IsVerbatimNodeKind();

            public bool Matches(RoslynNodeContext context) {
                return context.IsToken
                    && context.AsToken().Text.StartsWith("@");
            }

            public string ToPathString() => "_IsVerbatim";
        }

        public class SymbolNodeKind : IRoslynNodeKind {
            private readonly SymbolKind _symbolKind;

            public SymbolNodeKind(SymbolKind symbolKind) {
                _symbolKind = symbolKind;
            }

            public bool Matches(RoslynNodeContext context) {
                var symbol = context.SemanticModel.GetSymbolInfo(context.AsNode());
                return symbol.Symbol?.Kind == _symbolKind;
            }

            public string ToPathString() => "SymbolKind:" + _symbolKind.ToString();
        }

        public class OperationNodeKind : IRoslynNodeKind {
            private readonly OperationKind _operationKind;

            public OperationNodeKind(OperationKind operationKind) {
                _operationKind = operationKind;
            }

            public bool Matches(RoslynNodeContext context) {
                var operation = context.SemanticModel.GetOperation(context.AsNode());
                return operation?.Kind == _operationKind;
            }

            public string ToPathString() => "OperationKind:" + _operationKind.ToString();
        }

        // Represents a value that's not in SyntaxKind enum -- likely because the
        // current branch is too old. It can't match anything, but it's still needed
        // so that Explanation file can be loaded without exceptions.
        private class UnknownNodeKind : IRoslynNodeKind {
            public static UnknownNodeKind Default { get; } = new UnknownNodeKind();

            public bool Matches(RoslynNodeContext context) => false;
            public string ToPathString() => "Unknown";
        }
    }
}
