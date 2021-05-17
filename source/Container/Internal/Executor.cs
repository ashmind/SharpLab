using System;
using System.IO;
using System.Reflection;

namespace SharpLab.Container.Internal {
    public class Executor {
        public string Execute(Stream stream) {
            using var context = new CustomAssemblyLoadContext(shouldShareAssembly: ShouldShareAssembly);

            var assembly = context.LoadFromStream(stream);
            var main = assembly.EntryPoint;
            if (main == null)
                throw new ArgumentException("Entry point not found in " + assembly, nameof(stream));

            var args = main.GetParameters().Length > 0 ? new object[] { Array.Empty<string>() } : null;
            main.Invoke(null, args);

            return "Hmm";
        }

        private bool ShouldShareAssembly(AssemblyName assemblyName) {
            return assemblyName.FullName != typeof(Console).Assembly.FullName;
        }
    }
}
