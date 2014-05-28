using System.IO;
using JetBrains.Annotations;

namespace TryRoslyn.Core.Processing {
    [ThreadSafe]
    public interface IDecompiler {
        void Decompile(Stream assemblyStream, TextWriter codeWriter, LanguageIdentifier language);
    }
}