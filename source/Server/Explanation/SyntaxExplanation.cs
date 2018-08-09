using SourcePath;
using SourcePath.Roslyn;

namespace SharpLab.Server.Explanation {
    public class SyntaxExplanation {
        public SyntaxExplanation(ISourcePath<RoslynNodeContext> path, string name, string text, string link) {
            Path = Argument.NotNull(nameof(path), path);
            Name = Argument.NotNullOrEmpty(nameof(name), name);
            Text = Argument.NotNullOrEmpty(nameof(text), text);
            Link = Argument.NotNullOrEmpty(nameof(link), link);
        }

        public ISourcePath<RoslynNodeContext> Path { get; }
        public string Name { get; }
        public string Text { get; }
        public string Link { get; }
    }
}