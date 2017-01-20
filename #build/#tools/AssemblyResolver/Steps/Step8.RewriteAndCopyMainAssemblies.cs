using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using AssemblyResolver.Common;
using Mono.Cecil;

namespace AssemblyResolver.Steps {
    public static class Step8 {
        public static void RewriteAndCopyMainAssemblies(
            IImmutableDictionary<AssemblyShortName, AssemblyDetails> mainAssemblies,
            string targetDirectoryPath,
            IImmutableDictionary<AssemblyShortName, AssemblyDetails> usedRoslynAssemblies
        ) {
            FluentConsole.White.Line("Rewriting and copying main assemblies…");
            foreach (var assembly in mainAssemblies.Values) {
                FluentConsole.Gray.Line($"  {assembly.Definition.Name.Name}");
                // ReSharper disable once AssignNullToNotNullAttribute
                var targetPath = Path.Combine(targetDirectoryPath, Path.GetFileName(assembly.Path));
                var rewritten = false;
                foreach (var reference in assembly.Definition.MainModule.AssemblyReferences) {
                    FluentConsole.Gray.Line($"    {reference.FullName}");
                    var roslynAssembly = usedRoslynAssemblies.GetValueOrDefault(reference.Name);
                    if (roslynAssembly == null)
                        continue;
                    if (RewriteReference(reference, roslynAssembly.Definition.Name))
                        FluentConsole.Gray.Line($"      => {roslynAssembly.Definition.Name.FullName}");
                    rewritten = true;
                }
                if (rewritten) {
                    assembly.Definition.Write(targetPath);
                    // TODO: set last write time if file is the same (by content)
                }
                else {
                    Copy.File(assembly.Path, targetPath);
                }
                Copy.PdbIfExists(assembly.Path, targetPath);
            }
        }

        private static bool RewriteReference(AssemblyNameReference reference, AssemblyNameReference newReference) {
            if (reference.Name != newReference.Name)
                throw new ArgumentException($"Reference {reference.Name} cannot be rewritten to {newReference.Name}.");
            if ((newReference.PublicKeyToken?.SequenceEqual(reference.PublicKeyToken) ?? (reference.PublicKeyToken == null)) && newReference.Version == reference.Version)
                return false;

            reference.PublicKeyToken = newReference.PublicKeyToken;
            reference.Version = newReference.Version;
            return true;
        }
    }
}
