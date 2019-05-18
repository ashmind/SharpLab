using System;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace SharpLab.Server.Common.Internal {
    public class AssemblyReferenceDiscoveryTaskSource {
        private IImmutableList<string>? _assemblyPaths;
        private Action<IImmutableList<string>>? _action;

        public AssemblyReferenceDiscoveryTaskSource() {
            Task = new AssemblyReferenceDiscoveryTask(this);
        }

        [NotNull] public AssemblyReferenceDiscoveryTask Task { get; }

        public void Complete([NotNull] IImmutableList<string> assemblyPaths) {
            Argument.NotNull(nameof(assemblyPaths), assemblyPaths);
            lock (this) {
                if (_assemblyPaths != null)
                    throw new InvalidOperationException();
                _assemblyPaths = assemblyPaths;
                _action?.Invoke(_assemblyPaths);
            }
        }

        public void ContinueWith([NotNull] Action<IImmutableList<string>> action) {
            Argument.NotNull(nameof(action), action);
            lock (this) {
                if (_action != null)
                    throw new InvalidOperationException($"Only one continuation is supported for {nameof(AssemblyReferenceDiscoveryTask)}.");
                _action = action;
                if (_assemblyPaths != null)
                    _action(_assemblyPaths);
            }
        }
    }
}
