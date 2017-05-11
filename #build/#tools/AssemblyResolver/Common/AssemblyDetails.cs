using System.IO;
using Mono.Cecil;

namespace AssemblyResolver.Common {
    using IO = System.IO;

    public class AssemblyDetails {
        public AssemblyDetails(string path, AssemblyDefinition definition) {
            Path = path;
            Definition = definition;
        }

        public string Path { get; }
        public AssemblyDefinition Definition { get; }

        public static AssemblyDetails ReadFrom(string path, bool readSymbolsIfExist) {
            var readSymbols = readSymbolsIfExist && File.Exists(IO.Path.ChangeExtension(path, "pdb"));
            var definition = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { ReadSymbols = readSymbols });
            return new AssemblyDetails(path, definition);
        }
    }
}