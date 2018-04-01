using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using AshMind.Extensions;
using Mono.Cecil;

namespace SharpLab.Server.Common {
    public class PreCachedAssemblyResolver : IAssemblyResolver {
        private readonly ConcurrentDictionary<string, AssemblyDefinition> _cache = new ConcurrentDictionary<string, AssemblyDefinition>();

        public PreCachedAssemblyResolver(IReadOnlyCollection<ILanguageAdapter> languages) {
            foreach (var language in languages) {
                language.ReferencedAssembliesTask.ContinueWith(assemblies => AddToCache(assemblies));
            }
        }

        private void AddToCache(IReadOnlyCollection<Assembly> assemblies) {
            foreach (var assembly in assemblies) {
                var definition = AssemblyDefinition.ReadAssembly(assembly.GetAssemblyFile().FullName);
                var name = definition.Name.Name;
                _cache.TryAdd(name, definition);
            }
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name) {
            if (!_cache.TryGetValue(name.Name, out var assembly))
                throw new Exception($"Assembly {name.Name} was not found in cache.");
            return assembly;
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

        public void Dispose() {
            throw new NotSupportedException();
        }
    }
}
