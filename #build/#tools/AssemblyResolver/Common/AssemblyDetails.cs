using Mono.Cecil;

namespace AssemblyResolver.Common {
    public class AssemblyDetails {
        public AssemblyDetails(string path, AssemblyDefinition definition) {
            Path = path;
            Definition = definition;
        }

        public string Path { get; }
        public AssemblyDefinition Definition { get; }

        public static AssemblyDetails ReadFrom(string path, bool readSymbols) {
            var definition = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { ReadSymbols = readSymbols });
            return new AssemblyDetails(path, definition);
        }
    }
}