using System;
using System.Reflection;
using System.Runtime.Loader;

namespace SharpLab.Container.Execution {
    internal class CustomAssemblyLoadContext : AssemblyLoadContext, IDisposable {
        public CustomAssemblyLoadContext() : base(isCollectible: true) {
        }

        protected override Assembly Load(AssemblyName assemblyName) {
            return Assembly.Load(assemblyName);
        }

        public void Dispose() {
            Unload();
        }
    }
}
