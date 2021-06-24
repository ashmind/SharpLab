using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ICSharpCode.Decompiler.Metadata;
using Mono.Cecil;
using SharpLab.Server.Common.Diagnostics;

namespace SharpLab.Server.Common {
    public class PreCachedAssemblyResolver : ICSharpCode.Decompiler.Metadata.IAssemblyResolver, Mono.Cecil.IAssemblyResolver {
        private readonly ConcurrentDictionary<string, PEFile> _peFileCache = new();
        private readonly ConcurrentDictionary<string, AssemblyDefinition> _cecilCache = new();

        public PreCachedAssemblyResolver(IReadOnlyCollection<ILanguageAdapter> languages) {
            foreach (var language in languages) {
                language.AssemblyReferenceDiscoveryTask.ContinueWith(assemblyPaths => AddToCaches(assemblyPaths));
            }
        }

        private void AddToCaches(IReadOnlyCollection<string> assemblyPaths) {
            PerformanceLog.Checkpoint("PreCachedAssemblyResolver.AddToCaches.Start");
            foreach (var path in assemblyPaths) {
                var file = new PEFile(path);
                _peFileCache.TryAdd(file.Name, file);

                var definition = AssemblyDefinition.ReadAssembly(path);
                _cecilCache.TryAdd(definition.Name.Name, definition);
            }
            PerformanceLog.Checkpoint("PreCachedAssemblyResolver.AddToCaches.End");
        }

        public PEFile? Resolve(IAssemblyReference reference) {
            if (!_peFileCache.TryGetValue(reference.Name, out var assembly)) {
                // F# assembly graph includes these for some reason
                if (reference.Name == "System.Security.Permissions")
                    return null;
                if (reference.Name == "System.Threading.AccessControl")
                    return null;

                throw new Exception($"Assembly {reference.Name} was not found in cache.");
            }
            return assembly;
        }

        public PEFile ResolveModule(PEFile mainModule, string moduleName) {
            throw new NotSupportedException();
        }

        public AssemblyDefinition Resolve(Mono.Cecil.AssemblyNameReference name) {
            if (!_cecilCache.TryGetValue(name.Name, out var assembly))
                throw new Exception($"Assembly {name.Name} was not found in cache.");
            return assembly;
        }

        public AssemblyDefinition Resolve(Mono.Cecil.AssemblyNameReference name, ReaderParameters parameters) {
            throw new NotSupportedException();
        }

        public AssemblyDefinition Resolve(string fullName) {
            throw new NotSupportedException();
        }

        public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters) {
            throw new NotSupportedException();
        }

        public bool IsGacAssembly(IAssemblyReference reference) => false;

        public void Dispose() {
        }
    }
}
