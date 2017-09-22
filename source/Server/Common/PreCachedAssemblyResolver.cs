using System;
using System.Collections.Generic;
using System.Threading;
using AshMind.Extensions;
using Mono.Cecil;

namespace SharpLab.Server.Common {
    public class PreCachedAssemblyResolver : IAssemblyResolver {
        public static IAssemblyResolver Instance { get; } = new PreCachedAssemblyResolver();
        private static readonly Lazy<IReadOnlyDictionary<string, AssemblyDefinition>> Cache = new Lazy<IReadOnlyDictionary<string, AssemblyDefinition>>(
            BuildAssemblyCache, LazyThreadSafetyMode.ExecutionAndPublication);

        private static IReadOnlyDictionary<string, AssemblyDefinition> BuildAssemblyCache() {
            var cache = new Dictionary<string, AssemblyDefinition>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (assembly.IsDynamic)
                    continue;

                var definition = AssemblyDefinition.ReadAssembly(assembly.GetAssemblyFile().FullName);
                var name = definition.Name.Name;
                if (!cache.ContainsKey(name))
                    cache.Add(name, definition);
            }
            return cache;
        }
        
        public AssemblyDefinition Resolve(AssemblyNameReference name) {
            return Cache.Value.GetValueOrDefault(name.Name)
                ?? throw new Exception("Assembly " + name.Name + " was not found in cache.");
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) {
            throw new NotSupportedException();
        }

        public AssemblyDefinition Resolve(string fullName) {
            throw new NotSupportedException();
        }

        public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters) {
            throw new NotSupportedException();
        }
    }
}
