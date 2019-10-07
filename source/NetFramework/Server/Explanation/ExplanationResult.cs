using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SharpLab.Server.Explanation {
    public class ExplanationResult {
        public ExplanationResult(IEnumerable<(SyntaxNodeOrToken, SyntaxExplanation)> explanations) {
            Explanations = explanations;
        }

        public IEnumerable<(SyntaxNodeOrToken fragment, SyntaxExplanation explanation)> Explanations { get; }
    }
}
