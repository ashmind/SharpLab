using System.Collections.Generic;

namespace SharpLab.Server.Common.Internal {
    public interface IAssemblyPathCollector {
        IReadOnlySet<string> SlowGetAllAssemblyPathsIncludingReferences(params string[] assemblyNames);
    }
}