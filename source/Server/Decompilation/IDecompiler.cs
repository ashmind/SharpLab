using System.IO;
using JetBrains.Annotations;

namespace TryRoslyn.Server.Decompilation {
    public interface IDecompiler {
        [NotNull] string LanguageName { get; }
        void Decompile([NotNull] Stream assemblyStream, [NotNull] TextWriter codeWriter);
    }
}