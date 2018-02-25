using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace SharpLab.Server.Common.Internal {
    public class ReferencedAssembliesLoadTaskSource {
        private IImmutableList<Assembly> _assemblies;
        private Action<IImmutableList<Assembly>> _action;

        public ReferencedAssembliesLoadTaskSource() {
            Task = new ReferencedAssembliesLoadTask(this);
        }

        [NotNull] public ReferencedAssembliesLoadTask Task { get; }

        public void Complete([NotNull] IImmutableList<Assembly> assemblies) {
            Argument.NotNull(nameof(assemblies), assemblies);
            lock (this) {
                if (_assemblies != null)
                    throw new InvalidOperationException();
                _assemblies = assemblies;
                _action?.Invoke(_assemblies);
            }
        }

        public void ContinueWith([NotNull] Action<IImmutableList<Assembly>> action) {
            Argument.NotNull(nameof(action), action);
            lock (this) {
                if (_action != null)
                    throw new InvalidOperationException($"Only one continuation is supported for {nameof(ReferencedAssembliesLoadTask)}.");
                _action = action;
                if (_assemblies != null)
                    _action(_assemblies);
            }
        }
    }
}
