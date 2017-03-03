using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Server.Compilation.Internal {
    public class MetadataReferenceCollector : IMetadataReferenceCollector {
        public IEnumerable<MetadataReference> SlowGetMetadataReferencesRecursive(params Assembly[] assemblies) {
            foreach (var assembly in SlowCollectAllReferencedAssemblies(assemblies)) {
                yield return MetadataReference.CreateFromFile(assembly.Location);
            }
        }

        private ISet<Assembly> SlowCollectAllReferencedAssemblies(params Assembly[] assemblies) {
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
