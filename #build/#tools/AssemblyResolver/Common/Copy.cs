using System.IO;
using IO = System.IO;

// ReSharper disable ArrangeStaticMemberQualifier

namespace AssemblyResolver.Common {
    public static class Copy {
        public static void PdbIfExists(string assemblyPath, string targetAssemblyPath) {
            var pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
            if (IO.File.Exists(pdbPath)) {
                // ReSharper disable AssignNullToNotNullAttribute
                Copy.File(pdbPath, Path.ChangeExtension(targetAssemblyPath, "pdb"));
                // ReSharper restore AssignNullToNotNullAttribute
            }
        }

        public static void File(string sourcePath, string targetPath) {
            IO.File.Copy(sourcePath, targetPath, true);
            Copy.LastWriteTime(sourcePath, targetPath);
        }

        public static void LastWriteTime(string sourcePath, string targetPath) {
            IO.File.SetLastWriteTimeUtc(targetPath, IO.File.GetLastWriteTimeUtc(sourcePath));
        }
    }
}
