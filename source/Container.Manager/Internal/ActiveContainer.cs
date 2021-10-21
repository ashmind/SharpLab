using System;
using System.Diagnostics;
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
            CancellableOutputStream = new CancellablePipeStream(container.OutputStream);
        }

        public Stream InputStream => _container.InputStream;
        public Stream CancellableOutputStream { get; private init; }
        public Process Process => _container.Process;

        public void Dispose() => _container.Dispose();
    }
}
