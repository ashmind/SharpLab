using System.IO;

namespace TryRoslyn.Core.Decompilation {
    public interface IDecompiler {
        LanguageIdentifier Language { get; }
        void Decompile(Stream assemblyStream, TextWriter codeWriter);
    }
}