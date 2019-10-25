using System.Diagnostics;
#if DEBUG
using System.IO;
using System.Threading;
#endif
using Mono.Cecil;

namespace SharpLab.Server.Common {
    public static class AssemblyLog {
        #if DEBUG 
        private static readonly AsyncLocal<string> _pathFormat = new AsyncLocal<string>();

        public static void Enable(string pathFormat) {
            _pathFormat.Value = pathFormat;
        }
        #endif

        [Conditional("DEBUG")]
        public static void Log(string stepName, AssemblyDefinition assembly) {
            #if DEBUG
            var format = _pathFormat.Value;
            if (format == null)
                return;

            var path = string.Format(format, stepName);
            var directoryPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            assembly.Write(path);
            #endif
        }
    }
}
