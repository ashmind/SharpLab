using System.IO;
using JetBrains.Annotations;

namespace TryRoslyn.Core.Decompilation {
    [ThreadSafe]
    public interface IDecompiler {
        LanguageIdentifier Language { get; }
        void Decompile(Stream assemblyStream, TextWriter codeWriter);
    }
}