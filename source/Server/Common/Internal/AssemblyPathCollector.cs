using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Mono.Cecil;

namespace SharpLab.Server.Common.Internal {
    public class AssemblyPathCollector : IAssemblyPathCollector {
        private readonly ConcurrentDictionary<string, Lazy<ResolvedAssemblyName?>> _resolvedAssemblyNameCache = new();
        private readonly string _applicationAssemblyBasePath;
        private readonly string _referenceAssemblyBasePath;

        public AssemblyPathCollector() {
            // TODO: consider passing this in through a Settings object
            _applicationAssemblyBasePath = AppContext.BaseDirectory;
            // "refs" are provided by <PreserveCompilationContext> in csproj
            _referenceAssemblyBasePath = Path.Combine(_applicationAssemblyBasePath, "refs");
        }

        public IReadOnlySet<string> SlowGetAllAssemblyPathsIncludingReferences(params string[] assemblyNames) {
            Argument.NotNullOrEmpty(nameof(assemblyNames), assemblyNames);

            var set = new HashSet<string>();
            foreach (var assemblyName in assemblyNames) {
                SlowCollectSelfAndReferencedAssemblyPaths(assemblyName, set, canSkipIfNotFound: false);
            }
            return set;
        }

        private void SlowCollectSelfAndReferencedAssemblyPaths(string assemblyName, ISet<string> set, bool canSkipIfNotFound) {
            if (assemblyName == "System.Private.CoreLib")
                assemblyName = "System.Runtime";

            var resolved = _resolvedAssemblyNameCache.GetOrAdd(
                assemblyName,
                new Lazy<ResolvedAssemblyName?>(
                    () => SlowResolveAssemblyNameWithReferences(assemblyName, canReturnNull: canSkipIfNotFound),
                    LazyThreadSafetyMode.ExecutionAndPublication
                )
            ).Value;
            if (resolved == null)
                return;
            if (!set.Add(resolved.Path))
                return;

            foreach (var referenceName in resolved.ReferenceNames) {
                try {
                    SlowCollectSelfAndReferencedAssemblyPaths(referenceName, set, canSkipIfNotFound: resolved.IsSdkAssembly);
                }
                catch (AssemblyResolutionException ex) {
                    throw new AssemblyResolutionException(ex.AssemblyNameChain.Add(assemblyName), ex.InnerException);
                }
                catch (Exception ex) {
                    throw new AssemblyResolutionException(ImmutableArray.Create(referenceName, assemblyName), ex);
                }
            }
        }

        private ResolvedAssemblyName? SlowResolveAssemblyNameWithReferences(string assemblyName, bool canReturnNull) {
            var assemblyFileName = assemblyName + ".dll";

            var assemblyPath = Path.Combine(_applicationAssemblyBasePath, assemblyFileName);
            var isSdkAssembly = false;
            if (!File.Exists(assemblyPath)) {
                assemblyPath = Path.Combine(_referenceAssemblyBasePath, assemblyFileName);
                isSdkAssembly = true;
                if (!File.Exists(assemblyPath)) {
                    if (canReturnNull)
                        return null;

                    throw new FileNotFoundException($"Assembly '{assemblyName}' was not found.");
                }
            }

            using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
            return new(
                assemblyPath,
                IsSdkAssembly: isSdkAssembly,
                assembly.MainModule.AssemblyReferences.Select(r => r.Name).ToImmutableArray()
            );
        }

        private record ResolvedAssemblyName(
            string Path, bool IsSdkAssembly, ImmutableArray<string> ReferenceNames
        );

        private class AssemblyResolutionException : Exception {
            private string? _message;

            public AssemblyResolutionException(ImmutableArray<string> assemblyNameChain, Exception innerException)
                : base("", innerException)
            {
                AssemblyNameChain = assemblyNameChain;
            }
            public ImmutableArray<string> AssemblyNameChain { get; }
            public override string Message => _message ??= $"Failed to resolve assembly reference chain '{string.Join("' -> '", AssemblyNameChain.Reverse())}': {InnerException!.Message}";
            public new Exception InnerException => base.InnerException!;
        }
    }
}
