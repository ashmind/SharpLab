using Microsoft.CodeAnalysis;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Decompilation.Internal {
    public interface IRoslynOperationPropertySerializer {
        void SerializeProperties(IOperation operation, IFastJsonWriter writer);
    }
}