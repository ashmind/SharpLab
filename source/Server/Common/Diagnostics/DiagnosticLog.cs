#if DEBUG
using System;
using System.IO;
using System.Threading;
using Mono.Cecil;

namespace SharpLab.Server.Common.Diagnostics {
    public static class DiagnosticLog {
        private static readonly AsyncLocal<Func<string, string>> _getPathByStepName = new();

        public static void Enable(Func<string, string> getPathByStepName) {
            _getPathByStepName.Value = getPathByStepName;
        }

        public static bool IsEnabled() {
            return _getPathByStepName.Value != null;
        }

        public static void LogAssembly(string stepName, ModuleDefinition module) {
            var path = GetLogPathWithoutExtension(stepName);
            if (path == null)
                return;
            module.Write(path + ".dll");
        }

        public static void LogAssembly(string stepName, MemoryStream assemblyStream, MemoryStream? symbolStream) {
            var path = GetLogPathWithoutExtension(stepName);
            if (path == null)
                return;
            File.WriteAllBytes(path + ".dll", assemblyStream.ToArray());
            if (symbolStream != null)
                File.WriteAllBytes(path + ".pdb", symbolStream.ToArray());
        }

        public static void LogText(string stepName, string text) {
            var path = GetLogPathWithoutExtension(stepName);
            if (path == null)
                return;
            File.WriteAllText(path + ".txt", text);
        }

        private static string? GetLogPathWithoutExtension(string stepName) {
            if (_getPathByStepName.Value is not {} getPathByStepName)
                return null;

            var path = getPathByStepName(stepName);
            var directoryPath = Path.GetDirectoryName(path);
            if (directoryPath != null && !Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            return path;
        }
    }
}
#endif