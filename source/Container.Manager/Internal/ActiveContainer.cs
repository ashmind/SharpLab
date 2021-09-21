using System.IO;
using Fragile;

namespace SharpLab.Container.Manager.Internal {
    public class ActiveContainer
    {
        public ActiveContainer(
            IProcessContainer container
        ) {
            Container = container;
        }

        public IProcessContainer Container { get; }
        public Stream InputStream => Container.InputStream;
        public Stream OutputStream => Container.OutputStream;
    }
}
