using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Core.Processing.Languages {
    [ThreadSafe]
    public interface ILanguageSetup {
        LanguageIdentifier Identifier { get; }
        string LanguageName { get; }
        ParseOptions GetParseOptions(SourceCodeKind kind);
        Compilation CreateLibraryCompilation(string assemblyName, bool optimizationsEnabled);
    }
}