using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.Metadata;
using Mono.Cecil;

namespace SharpLab.Server.Common {
    public class PreCachedAssemblyResolver : ICSharpCode.Decompiler.Metadata.IAssemblyResolver, Mono.Cecil.IAssemblyResolver {
        private static readonly Task<PEFile?> NullFileTask = Task.FromResult((PEFile?)null);

        private readonly ConcurrentDictionary<string, (PEFile file, Task<PEFile> task)> _peFileCache = new();
        private readonly ConcurrentDictionary<string, AssemblyDefinition> _cecilCache = new();

        public PreCachedAssemblyResolver(IReadOnlyCollection<ILanguageAdapter> languages) {
            foreach (var language in languages) {
                language.AssemblyReferenceDiscoveryTask.ContinueWith(assemblyPaths => AddToCaches(assemblyPaths));
            }
        }

        private void AddToCaches(IReadOnlyCollection<string> assemblyPaths) {
            foreach (var path in assemblyPaths) {
                var file = new PEFile(path);
                _peFileCache.TryAdd(file.Name, (file, Task.FromResult(file)));

                var definition = AssemblyDefinition.ReadAssembly(path);
                _cecilCache.TryAdd(definition.Name.Name, definition);
            }
        }

        public PEFile? Resolve(IAssemblyReference reference) {
            return ResolveFromCache(reference).file;
        }

        public Task<PEFile?> ResolveAsync(IAssemblyReference reference) {
            return ResultAsNullable(ResolveFromCache(reference).task);
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

        public Task<PEFile?> ResolveModuleAsync(PEFile mainModule, string moduleName) {
            throw new NotSupportedException();
        }

        private (PEFile file, Task<PEFile> task) ResolveFromCache(IAssemblyReference reference) {
            if (!_peFileCache.TryGetValue(reference.Name, out var cached))
                throw new Exception($"Assembly {reference.Name} was not found in cache.");

            return (cached.file, cached.task);
        }

        private Task<PEFile?> ResultAsNullable(Task<PEFile> task) => (Task<PEFile?>)(object)task;

        public void Dispose() {
        }
    }
}
