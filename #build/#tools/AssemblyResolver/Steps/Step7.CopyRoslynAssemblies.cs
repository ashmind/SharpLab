using System.Collections.Immutable;
using System.IO;
using AssemblyResolver.Common;

namespace AssemblyResolver.Steps {
    public static class Step7 {
        public static void CopyRoslynAssemblies(IImmutableDictionary<AssemblyShortName, AssemblyDetails> usedRoslynAssemblies, string targetDirectoryPath) {
            FluentConsole.White.Line("Copying Roslyn assemblies…");
            foreach (var assembly in usedRoslynAssemblies.Values) {
                FluentConsole.Gray.Line($"  {assembly.Definition.Name.Name}");
                // ReSharper disable once AssignNullToNotNullAttribute
                var targetPath = Path.Combine(targetDirectoryPath, Path.GetFileName(assembly.Path));
                Copy.File(assembly.Path, targetPath);
                Copy.PdbIfExists(assembly.Path, targetPath);
            }
        }
    }
}
