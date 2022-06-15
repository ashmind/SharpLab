using System.IO;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;

namespace SharpLab.Server.Decompilation {
    public interface IDecompiler {
        string LanguageName { get; }
        void Decompile(CompilationStreamPair streams, TextWriter codeWriter, IWorkSession session);
    }
}