using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Core.Processing.Languages.Internal {
    public interface IMetadataReferenceCollector {
        IEnumerable<MetadataReference> SlowGetMetadataReferencesRecursive(params Assembly[] assemblies);
    }
}