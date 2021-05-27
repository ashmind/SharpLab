using System;
using System.IO;
using System.Runtime.Loader;

namespace SharpLab.Container.Execution {
    internal class Executor {
        private static readonly object[] EmptyMainArguments = new object[] { Array.Empty<string>() };

        public void Execute(Stream stream) {
            var context = new AssemblyLoadContext("ExecutorContext", isCollectible: true);
            try {
                var assembly = context.LoadFromStream(stream);
                var main = assembly.EntryPoint;
                if (main == null)
                    throw new ArgumentException("Entry point not found in " + assembly, nameof(stream));

                var args = main.GetParameters().Length > 0 ? EmptyMainArguments : null;
                main.Invoke(null, args);
            }
            finally {
                context.Unload();
            }
        }
    }
}
