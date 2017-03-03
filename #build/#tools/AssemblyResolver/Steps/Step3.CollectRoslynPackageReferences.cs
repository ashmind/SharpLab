using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AshMind.Extensions;
using AssemblyResolver.Common;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;

namespace AssemblyResolver.Steps {
    public static class Step3 {
        public static void CollectRoslynPackageReferences(
            string sourceRemapFromPath,
            string sourceRemapToPath,
            IEnumerable<AssemblyDetails> roslynAssemblies,
            out IImmutableDictionary<AssemblyShortName, IImmutableSet<PackageInfo>> roslynPackageMap
        ) {
            var map = new Dictionary<AssemblyShortName, ISet<PackageInfo>>();
            var lockJsonPaths = GetLockJsonPaths(roslynAssemblies, sourceRemapFromPath, sourceRemapToPath);

            FluentConsole.White.Line("Analyzing Roslyn project.lock.json files…");
            foreach (var projectLockJsonPath in lockJsonPaths) {
                var json = JObject.Parse(File.ReadAllText(projectLockJsonPath));
                foreach (var library in json.Value<JObject>("libraries")) {
                    var keyParts = library.Key.Split('/');
                    var id = keyParts[0];
                    var version = keyParts[1];

                    var files = library.Value.Value<JArray>("files");
                    if (files == null)
                        continue;
                    foreach (var path in files.Values<string>()) {
                        var dllName = Path.GetFileName(path);
                        // ReSharper disable once PossibleNullReferenceException
                        if (!dllName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var name = dllName.Substring(0, dllName.Length - ".dll".Length);
                        var packageInfo = new PackageInfo(id, version, projectLockJsonPath);
                        var packageInfoSet = map.GetOrAdd(name, _ => new HashSet<PackageInfo>());
                        if (packageInfoSet.Contains(packageInfo))
                            continue;

                        FluentConsole.Gray.Line($"  {name}");
                        FluentConsole.Gray.Line($"    {id}.{version}");
                        packageInfoSet.Add(packageInfo);
                    }
                }
            }
            roslynPackageMap = map.ToImmutableDictionary(
                m => m.Key,
                m => (IImmutableSet<PackageInfo>)m.Value.ToImmutableHashSet()
            );
        }

        private static IReadOnlyCollection<string> GetLockJsonPaths(
            IEnumerable<AssemblyDetails> roslynAssemblies,
            string remapFromPath, string remapToPath
        ) {
            FluentConsole.White.Line("Finding project.lock.json for each Roslyn assembly…");
            var lockJsonPaths = new HashSet<string>();
            foreach (var assembly in roslynAssemblies) {
                FluentConsole.Gray.Line($"  {assembly.Definition.Name}");
                var path = assembly.Definition.MainModule.Types
                    .SelectMany(t => t.Methods)
                    .Where(m => m.HasBody)
                    .Select(m => m.Body)
                    .SelectMany(b => b.Instructions)
                    .Select(i => i.SequencePoint?.Document.Url)
                    .First(u => u != null && !Regex.IsMatch(u, @"[\\/]obj(?:[\\/]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
                path = RemapPath(path, remapFromPath, remapToPath);

                var directoryPath = path;
                string lockJsonPath;
                var tried = new List<string>();
                do {
                    directoryPath = Path.GetDirectoryName(directoryPath);
                    if (directoryPath == null)
                        throw new FileNotFoundException($"Could not find project.lock.json file for assembly {assembly.Path}. Tried:  \r\n{string.Join("  \r\n", tried)}");
                    lockJsonPath = Path.Combine(directoryPath, "project.lock.json");
                    tried.Add(lockJsonPath);
                } while (!File.Exists(lockJsonPath));
                FluentConsole.Gray.Line($"    {lockJsonPath}");
                lockJsonPaths.Add(lockJsonPath);
            }
            return lockJsonPaths;
        }

        private static string RemapPath(string path, string rootFromPath, string rootToPath) {
            return new Regex("^" + Regex.Escape(rootFromPath), RegexOptions.IgnoreCase)
                .Replace(path, rootToPath);
        }
    }
}
