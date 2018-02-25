using System;
using System.Collections.Immutable;
using System.Reflection;
using JetBrains.Annotations;

namespace SharpLab.Server.Common.Internal {
    public class ReferencedAssembliesLoadTask {
        private readonly ReferencedAssembliesLoadTaskSource _source;

        public ReferencedAssembliesLoadTask(ReferencedAssembliesLoadTaskSource source) {
            _source = source;
        }

        public void ContinueWith([NotNull] Action<IImmutableList<Assembly>> action) {
            _source.ContinueWith(action);
        }
    }
}
