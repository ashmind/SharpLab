using System;
using System.IO;
using Fragile;

namespace SharpLab.Container.Manager.Internal {
    public class ActiveContainer : IDisposable
    {
        private readonly IProcessContainer _container;

        public ActiveContainer(
            IProcessContainer container
        ) {
            _container = container;
            CancellableInputStream = new CancellableInputStream(container.InputStream);
            CancellableOutputStream = new CancellableOutputStream(container.OutputStream);
        }

        public CancellableInputStream CancellableInputStream { get; private init; }
        public Stream CancellableOutputStream { get; private init; }
        public int FailureCount { get; set; }

        public bool HasExited() {
            try {
                return _container.Process.HasExited;
            }
            // If process has exited a while ago and handle is no longer functional
            catch (InvalidOperationException) {
                return true;
            }
        }

        public void Dispose() => _container.Dispose();
    }
}
