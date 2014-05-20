using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    [ThreadSafe]
    public interface IRoslynLanguage {
        LanguageIdentifier Identifier { get; }
        SyntaxTree ParseText(string code, SourceCodeKind kind);
        Compilation CreateUnsafeLibraryCompilation(string assemblyName, bool optimizationsEnabled);
    }
}