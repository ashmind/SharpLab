using System.IO;

namespace TryRoslyn.Core.Processing {
    public interface IDecompiler {
        void Decompile(Stream assemblyStream, TextWriter codeWriter);
    }
}