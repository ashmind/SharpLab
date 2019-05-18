using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace SharpLab.Server.AspNetCore.Platform {
    public class CustomAssemblyLoadContext : AssemblyLoadContext, IDisposable {
        private readonly Func<AssemblyName, bool> _shouldShareAssembly;

        public CustomAssemblyLoadContext(Func<AssemblyName, bool> shouldShareAssembly)
            : base(isCollectible: true) {
            _shouldShareAssembly = shouldShareAssembly;
        }

        protected override Assembly Load(AssemblyName assemblyName) {
            if (assemblyName.Name == "netstandard" || assemblyName.Name == "mscorlib" || assemblyName.Name.StartsWith("System.") || _shouldShareAssembly(assemblyName))
                return Assembly.Load(assemblyName);

            return LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory, assemblyName.Name + ".dll"));
        }

        public void Dispose() {
            Unload();
        }
    }
}
