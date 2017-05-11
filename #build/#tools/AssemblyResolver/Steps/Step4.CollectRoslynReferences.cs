using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using AssemblyResolver.Common;

namespace AssemblyResolver.Steps {
    public static class Step4 {
        private static readonly string LocalNuGetCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget/packages");
        private static readonly IReadOnlyDictionary<string, int> SupportedFrameworkNames = new[] {
                "net46", "net45",
                "netstandard1.0",
                "portable-net45+win8",
                "portable-net45+win8+wp8+wpa81",
                "portable-net40+sl4+win8+wp8"
            }
            .Select((name, index) => new { name, index }).ToDictionary(x => x.name, x => x.index);

        public static void CollectRoslynReferences(
            ref IImmutableDictionary<AssemblyShortName, AssemblyDetails> usedRoslynAssemblies,
            IImmutableDictionary<AssemblyShortName, string> roslynAssemblyPaths,
            ref IImmutableDictionary<AssemblyShortName, AssemblyDetails> mainAssemblies,
            IImmutableDictionary<AssemblyShortName, IImmutableSet<PackageInfo>> roslynPackageMap,
            out IImmutableDictionary<AssemblyShortName, AssemblyDetails> othersReferencedByRoslyn
        ) {
            var othersReferencedByRoslynBuilder = ImmutableSortedDictionary.CreateBuilder<AssemblyShortName, AssemblyDetails>();
            FluentConsole.White.Line("Analyzing Roslyn references…");
            var seen = new HashSet<AssemblyShortName>();
            var queue = new Queue<AssemblyDetails>(usedRoslynAssemblies.Values);
            while (queue.Count > 0) {
                var assembly = queue.Dequeue();
                FluentConsole.Gray.Line($"  {assembly.Definition.Name.Name}");
                seen.Add(assembly.Definition.Name.Name);
                foreach (var reference in assembly.Definition.MainModule.AssemblyReferences) {
                    if (!seen.Add(reference.Name))
                        continue;
                    FluentConsole.Gray.Line($"    {reference.FullName}");
                    mainAssemblies = mainAssemblies.Remove(reference.Name);
                    if (usedRoslynAssemblies.ContainsKey(reference.Name)) {
                        FluentConsole.Gray.Line("      [roslyn assembly, already used]");
                        continue;
                    }
                    var roslynAssemblyPath = roslynAssemblyPaths.GetValueOrDefault(reference.Name);
                    if (roslynAssemblyPath != null) {
                        FluentConsole.Gray.Line("      [roslyn assembly, queued]");
                        var roslynAssembly = AssemblyDetails.ReadFrom(roslynAssemblyPath, readSymbolsIfExist: true);
                        usedRoslynAssemblies = usedRoslynAssemblies.Add(roslynAssembly.Definition.Name.Name, roslynAssembly);
                        queue.Enqueue(roslynAssembly);
                        continue;
                    }
                    if (InGlobalAssemblyCache(reference)) {
                        FluentConsole.Gray.Line("      [gac]");
                        continue;
                    }

                    var referencedAssembly = GetAssemblyDetailsFromNuGetCache(reference.Name, roslynPackageMap);
                    if (referencedAssembly == null) {
                        FluentConsole.Gray.Line("      [system?]");
                        continue;
                    }
                    othersReferencedByRoslynBuilder.Add(reference.Name, referencedAssembly);
                    queue.Enqueue(referencedAssembly);
                }
            }
            othersReferencedByRoslyn = othersReferencedByRoslynBuilder.ToImmutable();
        }

        private static AssemblyDetails GetAssemblyDetailsFromNuGetCache(
            AssemblyShortName name,
            IImmutableDictionary<AssemblyShortName, IImmutableSet<PackageInfo>> roslynPackageMap
        ) {
            var packageInfoSet = roslynPackageMap.GetValueOrDefault(name);
            if (packageInfoSet == null) {
                throw new Exception($"Could not identify NuGet package for assembly '{name}' (in packages referenced by Roslyn).");
            }
            if (packageInfoSet.Count > 1)
                throw new Exception($"Ambiguous match for NuGet package for assembly '{name}':\r\n  {string.Join("\r\n  ", packageInfoSet)}.");

            var packageInfo = packageInfoSet.Single();
            FluentConsole.Gray.Line($"      {packageInfo.PackageId}.{packageInfo.PackageVersion}");
            var packageVersionPath = Path.Combine(LocalNuGetCachePath, packageInfo.PackageId, packageInfo.PackageVersion);

            var libPath = Path.Combine(packageVersionPath, "lib");
            var frameworkPath = new DirectoryInfo(libPath)
                .EnumerateDirectories()
                .Where(f => SupportedFrameworkNames.ContainsKey(f.Name))
                .OrderBy(f => SupportedFrameworkNames[f.Name])
                .FirstOrDefault()
                ?.FullName;
            if (frameworkPath == null)
                throw new DirectoryNotFoundException($"Could not find compatible framework for assembly '{name}' in NuGet cache, under {libPath}.");
            if (File.Exists(Path.Combine(frameworkPath, "_._")))
                return null;

            var assemblyPath = Path.Combine(frameworkPath, name.Name + ".dll");

            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException($"Could not find assembly '{name}' in NuGet cache, at {assemblyPath}.", assemblyPath);
            return AssemblyDetails.ReadFrom(assemblyPath, readSymbolsIfExist: false);
        }

        private static bool InGlobalAssemblyCache(AssemblyNameReference reference) {
            try {
                return Assembly.ReflectionOnlyLoad(reference.FullName).GlobalAssemblyCache;
            }
            catch (FileNotFoundException) {
                return false;
            }
            catch (FileLoadException) {
                return false;
            }
        }
    }
}
