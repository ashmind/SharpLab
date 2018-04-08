using System;
using Microsoft.CodeAnalysis;

namespace SharpLab.Server.Explanation {
    public class SyntaxExplanation {
        private readonly SyntaxFragmentScope _fragmentScope;

        public SyntaxExplanation(string name, string text, string link, SyntaxFragmentScope fragmentScope) {
            Name = Argument.NotNullOrEmpty(nameof(name), name);
            Text = Argument.NotNullOrEmpty(nameof(text), text);
            Link = link;
            _fragmentScope = fragmentScope;
        }

        public SyntaxNodeOrToken GetBestFragment(SyntaxNodeOrToken match) {
            switch (_fragmentScope) {
                case SyntaxFragmentScope.Self: return match;
                case SyntaxFragmentScope.Parent: return match.Parent;
                default: throw new ArgumentOutOfRangeException($"Unknown fragment scope: '{_fragmentScope}'.");
            }
        }

        public string Name { get; }
        public string Text { get; }
        public string Link { get; }
    }
}