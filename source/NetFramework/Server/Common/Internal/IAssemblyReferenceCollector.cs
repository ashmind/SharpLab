using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;

namespace SharpLab.Server.Common.Internal {
    public interface IAssemblyReferenceCollector {
        [NotNull] ISet<Assembly> SlowGetAllReferencedAssembliesRecursive([NotNull] params Assembly[] assemblies);
    }
}