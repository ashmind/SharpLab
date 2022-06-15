using System.IO;

namespace SharpLab.Server.Decompilation.Internal {
    public interface IILDecompiler : IDecompiler {
        void Decompile(Stream assemblyStream, Stream? symbolStream, TextWriter codeWriter);
    }
}
