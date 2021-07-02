using System;
using System.Diagnostics;
using System.IO;
#if DEBUG
using System.Threading;
#endif
using Mono.Cecil;

namespace SharpLab.Server.Common.Diagnostics {
    public static class AssemblyLog {
        #if DEBUG
        private static readonly AsyncLocal<Func<string, string>> _getPathByStepName = new();

        public static void Enable(Func<string, string> getPathByStepName) {
            _getPathByStepName.Value = getPathByStepName;
        }
        #endif

        [Conditional("DEBUG")]
        public static void Log(string stepName, AssemblyDefinition assembly) {
            #if DEBUG
            var path = GetLogPathWithoutExtension(stepName);
            if (path == null)
                return;
            assembly.Write(path + ".dll");
            #endif
        }

        public static void Log(string stepName, MemoryStream assemblyStream, MemoryStream? symbolStream) {
            #if DEBUG
            var path = GetLogPathWithoutExtension(stepName);
            if (path == null)
                return;
            File.WriteAllBytes(path + ".dll", assemblyStream.ToArray());
            if (symbolStream != null)
                File.WriteAllBytes(path + ".pdb", symbolStream.ToArray());
            #endif
        }

        #if DEBUG
        private static string? GetLogPathWithoutExtension(string stepName) {
            if (_getPathByStepName.Value is not {} getPathByStepName)
                return null;

            var path = getPathByStepName(stepName);
            var directoryPath = Path.GetDirectoryName(path);
            if (directoryPath != null && !Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            return path;
        }
        #endif
    }
}
