using System.Collections.Immutable;
using System.IO;
using AssemblyResolver.Common;

namespace AssemblyResolver.Steps {
    public static class Step2 {
        public static void CollectRoslynAssemblies(
            string roslynBinariesDirectoryPath,
            ref IImmutableDictionary<AssemblyShortName, AssemblyDetails> mainAssemblies,
            out IImmutableDictionary<AssemblyShortName, AssemblyDetails> usedRoslynAssemblies,
            out IImmutableDictionary<AssemblyShortName, string> roslynAssemblyPaths
        ) {
            var roslynAssemblyPathsBuilder = ImmutableSortedDictionary.CreateBuilder<AssemblyShortName, string>();
            var usedRoslynAssembliesBuilder = ImmutableSortedDictionary.CreateBuilder<AssemblyShortName, AssemblyDetails>();

            FluentConsole.White.Line($"Scanning {roslynBinariesDirectoryPath}…");
            foreach (var assemblyPath in Directory.EnumerateFiles(roslynBinariesDirectoryPath, "*.dll", SearchOption.AllDirectories)) {
                FluentConsole.Gray.Line($"  {Path.GetFileName(assemblyPath)}");
                var name = Path.GetFileNameWithoutExtension(assemblyPath);
                // ReSharper disable once AssignNullToNotNullAttribute
                var assemblyFromMain = mainAssemblies.GetValueOrDefault(name);
                if (assemblyFromMain != null) {
                    FluentConsole.Gray.Line("    [used by main]");
                    usedRoslynAssembliesBuilder.Add(name, AssemblyDetails.ReadFrom(assemblyPath, readSymbols: true));
                    mainAssemblies = mainAssemblies.Remove(name);
                }
                if (roslynAssemblyPathsBuilder.ContainsKey(name))
                    continue;
                roslynAssemblyPathsBuilder.Add(name, assemblyPath);
            }
            usedRoslynAssemblies = usedRoslynAssembliesBuilder.ToImmutable();
            roslynAssemblyPaths = roslynAssemblyPathsBuilder.ToImmutable();
        }
    }
}
