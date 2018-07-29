using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using AshMind.Extensions;
using ICSharpCode.Decompiler.Metadata;
using Mono.Cecil;

namespace SharpLab.Server.Common {
    public class PreCachedAssemblyResolver : ICSharpCode.Decompiler.Metadata.IAssemblyResolver, Mono.Cecil.IAssemblyResolver {
        private readonly ConcurrentDictionary<string, PEFile> _peFileCache = new ConcurrentDictionary<string, PEFile>();
        private readonly ConcurrentDictionary<string, AssemblyDefinition> _cecilCache = new ConcurrentDictionary<string, AssemblyDefinition>();

        public PreCachedAssemblyResolver(IReadOnlyCollection<ILanguageAdapter> languages) {
            foreach (var language in languages) {
                language.ReferencedAssembliesTask.ContinueWith(assemblies => AddToCaches(assemblies));
            }
        }

        private void AddToCaches(IReadOnlyCollection<Assembly> assemblies) {
            foreach (var assembly in assemblies) {
                var file = new PEFile(assembly.GetAssemblyFile().FullName);
                _peFileCache.TryAdd(file.Name, file);

                var definition = AssemblyDefinition.ReadAssembly(assembly.GetAssemblyFile().FullName);
                _cecilCache.TryAdd(definition.Name.Name, definition);
            }
        }

        public PEFile Resolve(IAssemblyReference reference) {
            if (!_peFileCache.TryGetValue(reference.Name, out var assembly))
                throw new Exception($"Assembly {reference.Name} was not found in cache.");
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

        public void Dispose() {
            throw new NotSupportedException();
        }
    }
}
