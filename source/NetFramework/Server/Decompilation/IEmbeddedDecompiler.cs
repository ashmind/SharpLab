using System.IO;

namespace SharpLab.Server.Decompilation {
    internal interface IEmbeddedDecompiler {
        void DecompileType(Stream assemblyStream, Stream? symbolStream, string reflectionTypeName, TextWriter codeWriter);
    }
}
