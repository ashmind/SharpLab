using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Server.Compilation {
    public interface ILanguageSetup {
        string LanguageName { get; }
        ParseOptions GetParseOptions();
        CompilationOptions GetCompilationOptions();
        ImmutableList<MetadataReference> GetMetadataReferences();
    }
}