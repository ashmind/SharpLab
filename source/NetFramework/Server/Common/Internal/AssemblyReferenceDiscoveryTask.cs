using System;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace SharpLab.Server.Common.Internal {
    public class AssemblyReferenceDiscoveryTask {
        private readonly AssemblyReferenceDiscoveryTaskSource _source;

        public AssemblyReferenceDiscoveryTask(AssemblyReferenceDiscoveryTaskSource source) {
            _source = source;
        }

        public void ContinueWith([NotNull] Action<IImmutableList<string>> action) {
            _source.ContinueWith(action);
        }
    }
}
