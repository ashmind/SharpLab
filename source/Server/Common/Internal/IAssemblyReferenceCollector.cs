using System.Collections.Generic;
using System.Reflection;

namespace SharpLab.Server.Common.Internal {
    public interface IAssemblyReferenceCollector {
        IReadOnlySet<Assembly> SlowGetAllReferencedAssembliesRecursive(params Assembly[] assemblies);
    }
}