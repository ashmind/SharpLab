using System.IO;

namespace AssemblyResolver.Steps {
    public static class Step5 {
        public static void CleanTargetDirectory(string targetDirectoryPath) {
            FluentConsole.White.Line($"Deleting {targetDirectoryPath}…");
            try {
                Directory.Delete(targetDirectoryPath, true);
            }
            catch (DirectoryNotFoundException) {
            }
            Directory.CreateDirectory(targetDirectoryPath);
        }
    }
}
