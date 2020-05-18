using System;
using System.Diagnostics;
#if DEBUG
using System.IO;
using System.Threading;
#endif
using ICSharpCode.Decompiler.Metadata;
using Mono.Cecil;

namespace SharpLab.Server.Common.Diagnostics {
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
            var path = GetLogPath(stepName);
            if (path == null)
                return;
            assembly.Write(path);
            #endif
        }

        public static void Log(string stepName, MemoryStream assemblyStream) {
            #if DEBUG
            var path = GetLogPath(stepName);
            if (path == null)
                return;
            File.WriteAllBytes(path, assemblyStream.ToArray());
            #endif
        }

        private static string? GetLogPath(string stepName) {
            var format = _pathFormat.Value;
            if (format == null)
                return null;

            var path = string.Format(format, stepName);
            var directoryPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            return path;
        }
    }
}
