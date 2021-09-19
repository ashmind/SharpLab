using System.Collections.Generic;
using System.Reflection;

namespace SharpLab.Server.Common.Internal {
    public class AssemblyReferenceCollector : IAssemblyReferenceCollector {
        public IReadOnlySet<Assembly> SlowGetAllReferencedAssembliesRecursive(params Assembly[] assemblies) {
            var set = new HashSet<Assembly>();
            foreach (var assembly in assemblies) {
                SlowCollectAllReferencedAssemblies(assembly, set);
            }
            return set;
        }

        private void SlowCollectAllReferencedAssemblies(Assembly assembly, ISet<Assembly> set) {
            if (!set.Add(assembly))
                return;

            foreach (var reference in assembly.GetReferencedAssemblies()) {
                SlowCollectAllReferencedAssemblies(Assembly.Load(reference), set);
            }
        }
    }
}
