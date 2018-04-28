using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourcePath;
using SourcePath.Roslyn;

namespace SharpLab.Server.Explanation.Internal {
    public class CSharpExplanationPathDialect : ISourcePathDialect<SyntaxNodeOrToken> {
        private static readonly IReadOnlyDictionary<string, RoslynCSharpNodeKind> NodeKinds =
            Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .ToDictionary(
                    k => k.ToString(),
                    k => new RoslynCSharpNodeKind(k.ToString(), new HashSet<SyntaxKind> { k })
                );

        public SourcePathDialectSupports Supports { get; } = new SourcePathDialectSupports {
            TopLevelAxis = false,
            TopLevelSegments = false,
            TopLevelAnd = false,
            AxisSelf = false,
            AxisDescendant = false,
            AxisParent = false,
            AxisAncestor = false
        };

        public ISourceNodeKind<SyntaxNodeOrToken> ResolveNodeKind(string nodeKindString) {
            Argument.NotNullOrEmpty(nameof(nodeKindString), nodeKindString);
            if (nodeKindString == "*")
                return StarNodeKind.Default;

            if (!NodeKinds.TryGetValue(nodeKindString, out var nodeKind))
                return UnknownNodeKind.Default;

            return nodeKind;
        }

        // Represents a value that's not in SyntaxKind enum -- likely because the
        // current branch is too old. It can't match anything, but it's still needed
        // so that Explanation file can be loaded without exceptions.
        private class UnknownNodeKind : ISourceNodeKind<SyntaxNodeOrToken> {
            public static UnknownNodeKind Default { get; } = new UnknownNodeKind();

            public bool Matches(SyntaxNodeOrToken node) => false;
            public string ToPathString() => "Unknown";
        }

        public class StarNodeKind : ISourceNodeKind<SyntaxNodeOrToken> {
            public static StarNodeKind Default { get; } = new StarNodeKind();

            public bool Matches(SyntaxNodeOrToken node) => true;
            public string ToPathString() => "*";
        }
    }
}
