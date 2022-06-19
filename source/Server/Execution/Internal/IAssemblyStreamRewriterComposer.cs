using System.IO;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;

namespace SharpLab.Server.Execution.Internal {
    public interface IAssemblyStreamRewriterComposer {
        Stream Rewrite(CompilationStreamPair streams, IWorkSession session);
    }
}