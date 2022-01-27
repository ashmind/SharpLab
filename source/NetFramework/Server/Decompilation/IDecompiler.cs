using System.IO;
using SharpLab.Server.Common;

namespace SharpLab.Server.Decompilation {
    internal interface IDecompiler {
        string LanguageName { get; }
        void Decompile(CompilationStreamPair streams, TextWriter codeWriter);
    }
}