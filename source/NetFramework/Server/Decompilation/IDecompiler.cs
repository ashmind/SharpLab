using System.IO;
using JetBrains.Annotations;
using SharpLab.Server.Common;

namespace SharpLab.Server.Decompilation {
    public interface IDecompiler {
        [NotNull] string LanguageName { get; }
        void Decompile([NotNull] CompilationStreamPair streams, [NotNull] TextWriter codeWriter);
    }
}