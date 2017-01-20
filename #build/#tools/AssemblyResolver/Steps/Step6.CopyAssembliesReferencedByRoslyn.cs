using System.Collections.Immutable;
using System.IO;
using AssemblyResolver.Common;

namespace AssemblyResolver.Steps {
    public static class Step6 {
        public static void CopyAssembliesReferencedByRoslyn(
            IImmutableDictionary<AssemblyShortName, AssemblyDetails> referenced,
            string targetDirectoryPath
        ) {
            FluentConsole.White.Line("Copying assemblies referenced by Roslyn…");
            foreach (var assembly in referenced.Values) {
                FluentConsole.Gray.Line($"    {assembly.Definition.FullName}");
                FluentConsole.Gray.Line($"      {assembly.Path}");
                // ReSharper disable once AssignNullToNotNullAttribute
                var targetPath = Path.Combine(targetDirectoryPath, Path.GetFileName(assembly.Path));
                Copy.File(assembly.Path, targetPath);
                Copy.PdbIfExists(assembly.Path, targetPath);
            }
        }
    }
}
