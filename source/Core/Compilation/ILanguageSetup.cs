using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Core.Compilation {
    public interface ILanguageSetup {
        string LanguageName { get; }
        ParseOptions GetParseOptions(SourceCodeKind kind);
        CompilationOptions GetCompilationOptions();
        ImmutableList<MetadataReference> GetMetadataReferences();
    }
}