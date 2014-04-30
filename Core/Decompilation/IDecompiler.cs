using System.IO;

namespace TryRoslyn.Core.Decompilation {
    public interface IDecompiler {
        void Decompile(Stream assemblyStream, TextWriter codeWriter);
    }
}