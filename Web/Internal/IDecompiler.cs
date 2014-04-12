using System.IO;
using ICSharpCode.Decompiler.Ast;

namespace TryRoslyn.Web.Internal {
    public interface IDecompiler {
        void Decompile(Stream assemblyStream, TextWriter codeWriter);
    }
}