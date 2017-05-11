using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using AshMind.Extensions;
using AssemblyResolver.Common;
using Newtonsoft.Json.Linq;

namespace AssemblyResolver.Steps {
    public static class Step3 {
        public static void CollectRoslynPackageReferences(
            string binariesDirectoryPath,
            out IImmutableDictionary<AssemblyShortName, IImmutableSet<PackageInfo>> roslynPackageMap
        ) {
            var map = new Dictionary<AssemblyShortName, ISet<PackageInfo>>();
            var depsJsonPaths = Directory.EnumerateFiles(binariesDirectoryPath, "*.deps.json", SearchOption.AllDirectories);

            FluentConsole.White.Line("Analyzing Roslyn *.deps.json files…");
            foreach (var depsJsonPath in depsJsonPaths) {
                var json = JObject.Parse(File.ReadAllText(depsJsonPath));
                foreach (var library in json.Value<JObject>("libraries")) {
                    if (library.Value.Value<string>("type") != "package")
                        continue;

                    var (id,version) = ParsePackageKey(library.Key);
                    MapToPackage(id, id, version, depsJsonPath, map);
                }

                foreach (var target in json.Value<JObject>("targets")) {
                    foreach (var package in (JObject)target.Value) {
                        var runtime = package.Value.Value<JObject>("runtime");
                        if (runtime == null)
                            continue;

                        foreach (var entry in runtime) {
                            var path = entry.Key;
                            if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                continue;

                            var name = Path.GetFileNameWithoutExtension(path);
                            var (id, version) = ParsePackageKey(package.Key);
                            MapToPackage(name, id, version, depsJsonPath, map);
                        }
                    }
                }
            }
            roslynPackageMap = map.ToImmutableDictionary(
                m => m.Key,
                m => (IImmutableSet<PackageInfo>)m.Value.ToImmutableHashSet()
            );
        }

        private static void MapToPackage(AssemblyShortName assemblyName, string id, string version, string depsJsonPath, IDictionary<AssemblyShortName, ISet<PackageInfo>> map) {
            var packageInfo = new PackageInfo(id, version, depsJsonPath);
            var packageInfoSet = map.GetOrAdd(assemblyName, _ => new HashSet<PackageInfo>());
            if (packageInfoSet.Contains(packageInfo))
                return;

            FluentConsole.Gray.Line($"  {id}");
            FluentConsole.Gray.Line($"    {id}.{version}");
            packageInfoSet.Add(packageInfo);
        }

        private static (string id, string version) ParsePackageKey(string key) {
            var parts = key.Split('/');
            return (parts[0], parts[1]);
        }
    }
}
